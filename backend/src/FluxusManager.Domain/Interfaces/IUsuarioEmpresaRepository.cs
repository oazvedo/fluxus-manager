using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IUsuarioEmpresaRepository : IRepository<UsuarioEmpresa>
{
    /// <summary>Indica se já há vínculo (ativo ou inativo) entre o usuário e a empresa.</summary>
    Task<bool> ExisteAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
    Task<UsuarioEmpresa?> ObterVinculoAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresa>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioEmpresa>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
