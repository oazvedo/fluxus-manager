using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task<UsuarioResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<UsuarioResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<UsuarioResponse> AtualizarAsync(Guid id, AtualizarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task AtivarAsync(Guid id, CancellationToken cancellationToken = default);

    Task InativarAsync(Guid id, CancellationToken cancellationToken = default);
}
