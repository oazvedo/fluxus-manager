using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FluxusManager.Application.Services;

/// <summary>
/// Decisão do administrador da plataforma. A aprovação roda sob bloqueio da solicitação e cria empresa, perfis padrão
/// e convite numa única transação: repetir ou aprovar em paralelo devolve o resultado já gravado.
/// </summary>
public class SolicitacaoCadastroAdminService(
    ISolicitacaoCadastroRepository solicitacoes,
    IEmpresaRepository empresas,
    IPerfilRepository perfis,
    IConviteRepository convites,
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    ISolicitacaoCadastroEmails emails,
    TimeProvider clock,
    IOptions<ConviteOptions> conviteOptions) : ISolicitacaoCadastroAdminService
{
    public const string CodigoCnpjJaCadastrado = "cnpj-ja-cadastrado";
    public const string CodigoStatusInvalido = "status-invalido";

    private DateTime Agora => clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<SolicitacaoResumoResponse>> ListarAsync(SolicitacaoCadastroStatus? status, DateOnly? de, DateOnly? ate,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var inicio = de?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var antesDe = ate?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var pagina = await solicitacoes.ListarAsync(status, inicio, antesDe, page, pageSize, cancellationToken);

        return pagina.Map(SolicitacaoResumoResponse.DeEntidade);
    }

    public async Task<SolicitacaoDetalheResponse> ObterAsync(Guid id, Guid administradorId, CancellationToken cancellationToken = default)
    {
        var solicitacao = await BuscarAsync(id, cancellationToken);

        solicitacao.RegistrarVisualizacao(administradorId);
        await unitOfWork.CommitAsync(cancellationToken);

        return await DetalheAsync(solicitacao, cancellationToken);
    }

    public async Task<SolicitacaoDetalheResponse> AprovarAsync(Guid id, AprovarSolicitacaoRequest request, Guid administradorId, CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await solicitacoes.BloquearAsync(id, ct);
                var solicitacao = await BuscarAsync(id, ct);
                if (solicitacao.Status == SolicitacaoCadastroStatus.Aprovada)
                    return;
                GarantirEmAnalise(solicitacao, "aprovada");
                if (await empresas.CnpjEmUsoAsync(solicitacao.Cnpj, ct))
                    throw CnpjJaCadastrado(solicitacao.Cnpj);

                var empresa = new Empresa(solicitacao.RazaoSocial, solicitacao.NomeFantasia, solicitacao.Cnpj);
                var administrador = Perfil.CriarPadrao(empresa.Id, "Administrador", "Acesso completo à empresa.", PermissionCatalog.All);
                var consulta = Perfil.CriarPadrao(empresa.Id, "Consulta", "Acesso de leitura aos cadastros.", PermissionCatalog.ReadOnly);
                // O token de verdade é gerado no envio do e-mail (Renovar); este só ocupa o campo e nunca sai daqui.
                var convite = new Convite(administrador, solicitacao.ResponsavelEmail, SecretToken.Hash(SecretToken.Create()),
                    clock.ExpiracaoEmSegundos(TimeSpan.FromHours(conviteOptions.Value.ValidadeHoras)));

                empresas.Add(empresa);
                perfis.Add(administrador);
                perfis.Add(consulta);
                convites.Add(convite);
                solicitacao.Aprovar(empresa.Id, convite.Id, administradorId, Opcional(request.ObservacaoInterna), Agora);
            }, cancellationToken);
        }
        catch (DuplicateKeyException ex)
        {
            // Empresa com o mesmo CNPJ criada por outro caminho entre a checagem e a gravação.
            throw new ConflictException("Já existe uma empresa com este CNPJ. A solicitação não foi aprovada.", ex) { Codigo = CodigoCnpjJaCadastrado };
        }

        return await EnviarEDetalharAsync(id, cancellationToken);
    }

    public async Task<SolicitacaoDetalheResponse> RecusarAsync(Guid id, RecusarSolicitacaoRequest request, Guid administradorId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await solicitacoes.BloquearAsync(id, ct);
            var solicitacao = await BuscarAsync(id, ct);
            if (solicitacao.Status == SolicitacaoCadastroStatus.Recusada)
                return;
            GarantirEmAnalise(solicitacao, "recusada");

            solicitacao.Recusar(request.Motivo.Trim(), administradorId, Opcional(request.ObservacaoInterna), Agora);
        }, cancellationToken);

        return await EnviarEDetalharAsync(id, cancellationToken);
    }

    public async Task<SolicitacaoDetalheResponse> ReenviarEmailsAsync(Guid id, Guid administradorId, CancellationToken cancellationToken = default)
    {
        var solicitacao = await BuscarAsync(id, cancellationToken);

        solicitacao.ReenviarEmails(administradorId, Agora);
        await unitOfWork.CommitAsync(cancellationToken);

        return await EnviarEDetalharAsync(id, cancellationToken);
    }

    /// <summary>Tenta os e-mails na hora, para o administrador ver o resultado; falhas ficam para o reenvio automático.</summary>
    private async Task<SolicitacaoDetalheResponse> EnviarEDetalharAsync(Guid id, CancellationToken cancellationToken)
    {
        await emails.ProcessarSolicitacaoAsync(id, cancellationToken);
        var atual = await solicitacoes.ObterDetalheAsync(id, somenteLeitura: true, cancellationToken)
            ?? throw new NotFoundException("Solicitação de cadastro", id);

        return await DetalheAsync(atual, cancellationToken);
    }

    private async Task<SolicitacaoDetalheResponse> DetalheAsync(SolicitacaoCadastro solicitacao, CancellationToken cancellationToken)
    {
        var ids = solicitacao.Eventos.Select(e => e.UsuarioId).Append(solicitacao.DecididaPorId)
            .OfType<Guid>().Distinct();
        var nomes = new Dictionary<Guid, string>();
        foreach (var usuarioId in ids)
            if (await usuarios.GetByIdAsync(usuarioId, cancellationToken) is { } usuario)
                nomes[usuarioId] = usuario.Nome;

        return SolicitacaoDetalheResponse.DeEntidade(solicitacao, nomes);
    }

    private static void GarantirEmAnalise(SolicitacaoCadastro solicitacao, string decisao)
    {
        if (solicitacao.Status != SolicitacaoCadastroStatus.PendenteAnalise)
            throw new ConflictException(solicitacao.Status == SolicitacaoCadastroStatus.AguardandoVerificacao
                ? $"O solicitante ainda não confirmou o e-mail. A solicitação só pode ser {decisao} depois disso."
                : $"Esta solicitação já foi decidida e não pode ser {decisao}.")
            { Codigo = CodigoStatusInvalido };
    }

    private static ConflictException CnpjJaCadastrado(string cnpj)
        => new($"Já existe uma empresa com o CNPJ {Cnpj.Formatar(cnpj)}. Recuse a solicitação informando o motivo.") { Codigo = CodigoCnpjJaCadastrado };

    private async Task<SolicitacaoCadastro> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await solicitacoes.ObterDetalheAsync(id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Solicitação de cadastro", id);

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
