using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IPerfilRepository : IRepository<Perfil>
{
    Task<Perfil?> ObterComPermissoesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Perfil?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<bool> TemVinculosAsync(Guid id, CancellationToken cancellationToken = default);
}
