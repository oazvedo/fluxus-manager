using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IUsuarioEmpresaRepository : IRepository<UsuarioEmpresa>
{
    Task<UsuarioEmpresa?> ObterVinculoAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresa>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresa>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
