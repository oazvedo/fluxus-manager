using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> ObterPorHashAsync(string hash, CancellationToken cancellationToken = default)
        => context.Set<RefreshToken>().AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

    public async Task BloquearFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("O bloqueio da família exige uma transação aberta.");

        // Lock distribuído pelo PostgreSQL, mantido até o commit/rollback. Abrange todos os descendentes,
        // inclusive os inseridos por outra requisição enquanto esta aguardava pelo lock.
        await context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({familiaId.ToString()}, 0))", cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> ListarFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default)
        => await context.Set<RefreshToken>().Where(t => t.FamiliaId == familiaId).ToListAsync(cancellationToken);

    public void Add(RefreshToken token) => context.Set<RefreshToken>().Add(token);

    public Task<int> LimparExpiradosAsync(DateTime agora, CancellationToken cancellationToken = default)
        // A limpeza não disputa famílias em rotação/logout no instante da expiração.
        // O lock dura a transação implícita deste DELETE; famílias ocupadas ficam para a próxima execução.
        => context.Database.ExecuteSqlAsync($"""
            DELETE FROM refresh_tokens
            WHERE expira_em <= {agora}
              AND pg_try_advisory_xact_lock(hashtextextended(familia_id::text, 0))
            """, cancellationToken);
}
