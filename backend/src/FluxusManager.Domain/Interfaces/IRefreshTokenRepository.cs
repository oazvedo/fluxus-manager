using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> ObterPorHashAsync(string hash, CancellationToken cancellationToken = default);
    /// <summary>Serializa rotação e logout da mesma família; exige transação aberta.</summary>
    Task BloquearFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> ListarFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);
    /// <summary>Remove fisicamente famílias vencidas. Tokens usados permanecem até a expiração da família para detectar reuso.</summary>
    Task<int> LimparExpiradosAsync(DateTime agora, CancellationToken cancellationToken = default);
}
