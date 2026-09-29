using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.Interfaces;

/// <summary>Análise das solicitações pelo administrador da plataforma (a autorização é da API).</summary>
public interface ISolicitacaoCadastroAdminService
{
    /// <param name="ate">Inclusive: o dia inteiro entra no filtro.</param>
    Task<PagedResult<SolicitacaoResumoResponse>> ListarAsync(SolicitacaoCadastroStatus? status, DateOnly? de, DateOnly? ate,
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Registra a leitura no histórico.</summary>
    Task<SolicitacaoDetalheResponse> ObterAsync(Guid id, Guid administradorId, CancellationToken cancellationToken = default);

    /// <summary>Cria a empresa, os perfis padrão e o convite do responsável uma única vez; repetir devolve o mesmo resultado.</summary>
    Task<SolicitacaoDetalheResponse> AprovarAsync(Guid id, AprovarSolicitacaoRequest request, Guid administradorId, CancellationToken cancellationToken = default);

    Task<SolicitacaoDetalheResponse> RecusarAsync(Guid id, RecusarSolicitacaoRequest request, Guid administradorId, CancellationToken cancellationToken = default);

    /// <summary>Tenta de novo os e-mails não entregues; nunca recria empresa ou convite.</summary>
    Task<SolicitacaoDetalheResponse> ReenviarEmailsAsync(Guid id, Guid administradorId, CancellationToken cancellationToken = default);
}
