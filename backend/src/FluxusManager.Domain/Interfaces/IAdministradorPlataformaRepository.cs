using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IAdministradorPlataformaRepository : IRepository<AdministradorPlataforma>
{
    /// <summary>O usuário tem o papel global e continua ativo.</summary>
    Task<bool> EhAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<AdministradorPlataforma?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
