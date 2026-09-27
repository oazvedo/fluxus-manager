using FluxusManager.Application.DTOs.UsuariosEmpresasDtos;

namespace FluxusManager.Application.Interfaces;

public interface IUsuarioEmpresaService
{
    Task<UsuarioEmpresaResponse> VincularAsync(VincularUsuarioEmpresaRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<UsuarioEmpresaResponse> AtualizarPerfilAsync(Guid usuarioId, Guid empresaId, AtualizarPerfilUsuarioEmpresaRequest request, CancellationToken cancellationToken = default);
    Task DesvincularAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
    Task AtivarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
    Task InativarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
}
