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
/// Pedido público de cadastro. Para não revelar clientes nem pedidos existentes, o envio responde sempre igual e
/// nunca envia e-mail durante a requisição: a entrega fica com a rotina em segundo plano.
/// </summary>
public class SolicitacaoCadastroService(
    ISolicitacaoCadastroRepository solicitacoes,
    IEmpresaRepository empresas,
    IUnitOfWork unitOfWork,
    ISolicitacaoCadastroEmails emails,
    TimeProvider clock,
    IOptions<SolicitacaoCadastroOptions> options) : ISolicitacaoCadastroService
{
    public const string CodigoTokenInvalido = "token-invalido";
    public const string CodigoTokenExpirado = "token-expirado";

    private DateTime Agora => clock.GetUtcNow().UtcDateTime;

    public async Task SolicitarAsync(CriarSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default)
    {
        var agora = Agora;
        var cnpj = Cnpj.Normalizar(request.Cnpj);
        var email = EnderecoEmail.Normalizar(request.ResponsavelEmail);

        // CNPJ que já é cliente: nada a fazer, e a resposta é a mesma.
        if (await empresas.CnpjEmUsoAsync(cnpj, cancellationToken))
            return;

        var ativas = await solicitacoes.ListarAtivasPorCnpjOuEmailAsync(cnpj, email, cancellationToken);
        var mesmoPedido = ativas.FirstOrDefault(s => s.Cnpj == cnpj && s.ResponsavelEmail == email);
        if (mesmoPedido is not null)
        {
            // Quem repete o pedido ainda sem verificar recebe um link novo; em análise, nada muda.
            if (mesmoPedido.Status == SolicitacaoCadastroStatus.AguardandoVerificacao && mesmoPedido.ReenviarVerificacao(agora))
            {
                await unitOfWork.CommitAsync(cancellationToken);
                emails.Sinalizar();
            }
            return;
        }

        // Pedido de outra pessoa (ou com outro CNPJ) que nunca foi verificado e já venceu não segura o CNPJ/e-mail.
        var validade = TimeSpan.FromHours(options.Value.VerificacaoValidadeHoras);
        var vencidas = ativas.Where(s => s.VerificacaoVencida(agora, validade)).ToList();
        if (vencidas.Count < ativas.Count)
            return;

        var solicitacao = new SolicitacaoCadastro(request.RazaoSocial.Trim(), Opcional(request.NomeFantasia), cnpj,
            request.ResponsavelNome.Trim(), email, Opcional(request.ResponsavelTelefone), agora);
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // Os vencidos saem antes, por causa dos índices únicos de pedido ativo por CNPJ e por e-mail.
                if (vencidas.Count > 0)
                {
                    vencidas.ForEach(solicitacoes.Remove);
                    await unitOfWork.CommitAsync(ct);
                }
                solicitacoes.Add(solicitacao);
            }, cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Outro envio simultâneo com o mesmo CNPJ ou e-mail chegou antes: a resposta continua a mesma.
            return;
        }

        emails.Sinalizar();
    }

    public async Task<VerificacaoSolicitacaoResponse> VerificarAsync(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default)
    {
        var agora = Agora;
        var solicitacao = await solicitacoes.ObterPorVerificacaoHashAsync(SecretToken.Hash(request.Token), cancellationToken)
            ?? throw TokenInvalido();
        if (solicitacao.VerificacaoExpirada(agora))
            throw new ExpiredException("Este link de confirmação venceu. Peça um novo link na página de cadastro.") { Codigo = CodigoTokenExpirado };

        solicitacao.Verificar(agora);
        await unitOfWork.CommitAsync(cancellationToken);
        emails.Sinalizar();

        return new VerificacaoSolicitacaoResponse(solicitacao.Status.ToString());
    }

    public async Task ReenviarVerificacaoAsync(ReenviarVerificacaoRequest request, CancellationToken cancellationToken = default)
    {
        var solicitacao = await solicitacoes.ObterAguardandoVerificacaoPorEmailAsync(EnderecoEmail.Normalizar(request.Email), cancellationToken);
        if (solicitacao is null || !solicitacao.ReenviarVerificacao(Agora))
            return;

        await unitOfWork.CommitAsync(cancellationToken);
        emails.Sinalizar();
    }

    public async Task<AcompanhamentoSolicitacaoResponse> AcompanharAsync(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default)
    {
        var solicitacao = await solicitacoes.ObterPorAcompanhamentoHashAsync(SecretToken.Hash(request.Token), cancellationToken)
            ?? throw TokenInvalido();
        if (solicitacao.AcompanhamentoExpiraEm <= Agora)
            throw new ExpiredException("Este link de acompanhamento venceu. Use o link do e-mail mais recente sobre o pedido.") { Codigo = CodigoTokenExpirado };

        return new AcompanhamentoSolicitacaoResponse(solicitacao.Status.ToString(), solicitacao.RazaoSocial, solicitacao.CriadoEm,
            solicitacao.VerificadaEm, solicitacao.DecididaEm, solicitacao.MotivoRecusa);
    }

    private static NotFoundException TokenInvalido()
        => new("Link inválido ou já usado. Abra o link do e-mail mais recente sobre o pedido.") { Codigo = CodigoTokenInvalido };

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
