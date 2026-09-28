using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class PasswordResetTokenRepository(AppDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> ObterParaUsoAsync(string hash, DateTime agora, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A validação do token exige uma transação aberta.");

        return context.Set<PasswordResetToken>()
            .FromSqlInterpolated($"SELECT * FROM password_reset_tokens WHERE token_hash = {hash} AND usado_em IS NULL AND expira_em > {agora} AND excluido = false FOR UPDATE")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(PasswordResetToken token) => context.Set<PasswordResetToken>().Add(token);
}
