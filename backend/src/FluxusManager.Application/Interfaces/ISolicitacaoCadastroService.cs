using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

namespace FluxusManager.Application.Interfaces;

/// <summary>Lado público da solicitação de cadastro. Nada aqui revela se um CNPJ ou e-mail já existe.</summary>
public interface ISolicitacaoCadastroService
{
    /// <summary>Registra o pedido (ou reenvia a verificação de um pedido igual em aberto). Nunca falha por duplicidade.</summary>
    Task SolicitarAsync(CriarSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default);

    Task<VerificacaoSolicitacaoResponse> VerificarAsync(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default);

    Task ReenviarVerificacaoAsync(ReenviarVerificacaoRequest request, CancellationToken cancellationToken = default);

    Task<AcompanhamentoSolicitacaoResponse> AcompanharAsync(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken = default);
}
