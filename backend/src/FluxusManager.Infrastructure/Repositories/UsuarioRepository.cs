using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext context) : RepositoryBase<Usuario>(context), IUsuarioRepository
{
    public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId = null, CancellationToken cancellationToken = default)
        => Set.AnyAsync(u => u.Email == email && (ignorarId == null || u.Id != ignorarId), cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}
