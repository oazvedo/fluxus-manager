using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface ISolicitacaoCadastroRepository : IRepository<SolicitacaoCadastro>
{
    /// <summary>Solicitações ativas (aguardando verificação ou em análise) com o CNPJ ou o e-mail informados.</summary>
    Task<List<SolicitacaoCadastro>> ListarAtivasPorCnpjOuEmailAsync(string cnpj, string email, CancellationToken cancellationToken = default);

    Task<SolicitacaoCadastro?> ObterAguardandoVerificacaoPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<SolicitacaoCadastro?> ObterPorVerificacaoHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<SolicitacaoCadastro?> ObterPorAcompanhamentoHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Com histórico e e-mails. <paramref name="somenteLeitura"/> lê o estado atual do banco, sem rastrear.</summary>
    Task<SolicitacaoCadastro?> ObterDetalheAsync(Guid id, bool somenteLeitura = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializa decisões sobre a mesma solicitação até o fim da transação atual (bloqueio do PostgreSQL):
    /// duas aprovações simultâneas viram uma aprovação e uma leitura do resultado.
    /// </summary>
    Task BloquearAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fila do administrador, da mais antiga para a mais nova, filtrada pela criação em [de, antesDe).</summary>
    Task<PagedResult<SolicitacaoCadastro>> ListarAsync(SolicitacaoCadastroStatus? status, DateTime? de, DateTime? antesDe,
        int page, int pageSize, CancellationToken cancellationToken = default);
}
