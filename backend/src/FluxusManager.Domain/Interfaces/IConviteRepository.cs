using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IConviteRepository : IRepository<Convite>
{
    Task<Convite?> ObterComPerfilAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Convite com status pendente (vencido ou não) para o e-mail na empresa atual.</summary>
    Task<Convite?> ObterPendentePorEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Busca em todas as empresas: quem aceita ainda não tem tenant. O chamador define o tenant a partir do convite.</summary>
    Task<Convite?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
