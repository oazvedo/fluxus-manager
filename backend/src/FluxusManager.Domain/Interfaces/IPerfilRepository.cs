using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IPerfilRepository : IRepository<Perfil>
{
    Task<Perfil?> ObterComPermissoesAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Perfil ativo da empresa atual; null se não existir, estiver inativo ou for de outra empresa.</summary>
    Task<Perfil?> ObterAtivoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Perfil?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    /// <summary>Indica se há convite pendente (vencido ou não) com este perfil.</summary>
    Task<bool> TemConvitesPendentesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TemVinculosAsync(Guid id, CancellationToken cancellationToken = default);
}
