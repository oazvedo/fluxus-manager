using FluxusManager.Application.DTOs.PerfisDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Interfaces;

public interface IPerfilService
{
    Task<PagedResult<PerfilResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PerfilResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PerfilResponse> CriarAsync(CriarPerfilRequest request, CancellationToken cancellationToken = default);
    Task<PerfilResponse> AtualizarAsync(Guid id, AtualizarPerfilRequest request, CancellationToken cancellationToken = default);
    Task AtivarAsync(Guid id, CancellationToken cancellationToken = default);
    Task InativarAsync(Guid id, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Guid id, CancellationToken cancellationToken = default);
    IReadOnlyCollection<PermissaoResponse> ListarPermissoes();
}
