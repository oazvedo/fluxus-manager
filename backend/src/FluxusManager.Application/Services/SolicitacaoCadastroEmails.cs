using System.Threading.Channels;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluxusManager.Application.Services;

/// <summary>
/// Outbox dos e-mails da solicitação de cadastro. Cada entrega roda num escopo próprio (fora da requisição que a pediu,
/// com o tenant da empresa aprovada quando precisa renovar o convite): marca "Enviando", gera o token novo do link,
/// envia e grava o token junto com "Enviado". Se algo falhar, o token novo é descartado e a falha fica agendada.
/// </summary>
public sealed class SolicitacaoCadastroEmails(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<SolicitacaoCadastroEmails> logger) : ISolicitacaoCadastroEmails
{
    private const int LoteMaximo = 50;

    private readonly Channel<bool> _sinal = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private DateTime Agora => clock.GetUtcNow().UtcDateTime;

    public void Sinalizar() => _sinal.Writer.TryWrite(true);

    public async Task AguardarSinalAsync(TimeSpan prazo, CancellationToken cancellationToken = default)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(prazo);
        try
        {
            await _sinal.Reader.ReadAsync(limite.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    public async Task<int> ProcessarDevidosAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> ids;
        await using (var scope = scopeFactory.CreateAsyncScope())
            ids = await scope.ServiceProvider.GetRequiredService<ISolicitacaoCadastroEmailRepository>()
                .ListarDevidosAsync(Agora, LoteMaximo, cancellationToken);

        foreach (var id in ids)
            await ProcessarAsync(id, cancellationToken);
        return ids.Count;
    }

    public async Task ProcessarSolicitacaoAsync(Guid solicitacaoId, CancellationToken cancellationToken = default)
    {
        List<Guid> ids;
        await using (var scope = scopeFactory.CreateAsyncScope())
            ids = await scope.ServiceProvider.GetRequiredService<ISolicitacaoCadastroEmailRepository>()
                .ListarNaoEntreguesAsync(solicitacaoId, cancellationToken);

        foreach (var id in ids)
            await ProcessarAsync(id, cancellationToken);
    }

    private async Task ProcessarAsync(Guid emailId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var email = await services.GetRequiredService<ISolicitacaoCadastroEmailRepository>().GetByIdAsync(emailId, cancellationToken);
        if (email is null || !email.IniciarEnvio(Agora))
            return;
        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            // Outro processo marcou o mesmo e-mail primeiro e cuida do envio.
            return;
        }

        try
        {
            var solicitacao = await services.GetRequiredService<ISolicitacaoCadastroRepository>().GetByIdAsync(email.SolicitacaoId, cancellationToken)
                ?? throw new InvalidOperationException("Solicitação do e-mail não encontrada.");
            var mensagem = await MontarAsync(services, email, solicitacao, cancellationToken);
            // Mensagem nula: o e-mail perdeu o sentido (ex.: verificação de um pedido já verificado).
            if (mensagem is not null)
                await services.GetRequiredService<IEmailSender>().EnviarAsync(mensagem, cancellationToken);

            email.MarcarEnviado();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Só o tipo da exceção: a mensagem do SMTP pode citar o destinatário.
            logger.LogWarning("Falha ao enviar o e-mail {Tipo} da solicitação de cadastro {SolicitacaoId} (tentativa {Tentativa}): {Erro}",
                email.Tipo, email.SolicitacaoId, email.Tentativas, ex.GetType().Name);
            await RegistrarFalhaAsync(emailId, cancellationToken);
        }
    }

    /// <summary>Em outro escopo: o token gerado para a mensagem que falhou não pode ser gravado.</summary>
    private async Task RegistrarFalhaAsync(Guid emailId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var email = await scope.ServiceProvider.GetRequiredService<ISolicitacaoCadastroEmailRepository>().GetByIdAsync(emailId, cancellationToken);
        if (email is null)
            return;

        email.MarcarFalha(Agora);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(cancellationToken);
    }

    private async Task<MensagemEmail?> MontarAsync(IServiceProvider services, SolicitacaoCadastroEmail email,
        SolicitacaoCadastro solicitacao, CancellationToken cancellationToken)
    {
        var opcoes = services.GetRequiredService<IOptions<SolicitacaoCadastroOptions>>().Value;
        var frontend = services.GetRequiredService<IOptions<FrontendOptions>>().Value;
        var token = SecretToken.Create();

        switch (email.Tipo)
        {
            case SolicitacaoCadastroEmailTipo.Verificacao:
                if (solicitacao.Status != SolicitacaoCadastroStatus.AguardandoVerificacao)
                    return null;
                var validade = TimeSpan.FromHours(opcoes.VerificacaoValidadeHoras);
                solicitacao.RenovarVerificacao(SecretToken.Hash(token), clock.ExpiracaoEmSegundos(validade));
                return EmailTemplates.VerificacaoSolicitacao(solicitacao.ResponsavelEmail, solicitacao.ResponsavelNome,
                    solicitacao.RazaoSocial, frontend.Link($"solicitar-cadastro/verificar?token={token}"), validade);

            case SolicitacaoCadastroEmailTipo.Acompanhamento:
                RenovarAcompanhamento(solicitacao, token, opcoes);
                return EmailTemplates.SolicitacaoEmAnalise(solicitacao.ResponsavelEmail, solicitacao.ResponsavelNome,
                    solicitacao.RazaoSocial, LinkAcompanhamento(frontend, token), opcoes.AcompanhamentoValidadeDias);

            case SolicitacaoCadastroEmailTipo.Recusa:
                RenovarAcompanhamento(solicitacao, token, opcoes);
                return EmailTemplates.SolicitacaoRecusada(solicitacao.ResponsavelEmail, solicitacao.ResponsavelNome,
                    solicitacao.RazaoSocial, solicitacao.MotivoRecusa!, LinkAcompanhamento(frontend, token));

            case SolicitacaoCadastroEmailTipo.Aprovacao:
                // O convite é da empresa criada: este escopo passa a operar nela para poder renová-lo.
                services.GetRequiredService<ITenantContext>().SetTenant(solicitacao.EmpresaId!.Value);
                var convite = await services.GetRequiredService<IConviteRepository>().ObterComPerfilAsync(email.ConviteId!.Value, cancellationToken);
                if (convite is not { Status: ConviteStatus.Pendente })
                    return null;
                var validadeConvite = TimeSpan.FromHours(services.GetRequiredService<IOptions<ConviteOptions>>().Value.ValidadeHoras);
                convite.Renovar(SecretToken.Hash(token), clock.ExpiracaoEmSegundos(validadeConvite));
                return EmailTemplates.SolicitacaoAprovada(solicitacao.ResponsavelEmail, solicitacao.ResponsavelNome,
                    solicitacao.RazaoSocial, frontend.Link($"convites/aceitar?token={token}"), validadeConvite);

            default:
                throw new InvalidOperationException($"Tipo de e-mail sem modelo: {email.Tipo}.");
        }
    }

    private void RenovarAcompanhamento(SolicitacaoCadastro solicitacao, string token, SolicitacaoCadastroOptions opcoes)
        => solicitacao.RenovarAcompanhamento(SecretToken.Hash(token),
            clock.ExpiracaoEmSegundos(TimeSpan.FromDays(opcoes.AcompanhamentoValidadeDias)));

    private static string LinkAcompanhamento(FrontendOptions frontend, string token)
        => frontend.Link($"solicitar-cadastro/acompanhar?token={token}");
}
