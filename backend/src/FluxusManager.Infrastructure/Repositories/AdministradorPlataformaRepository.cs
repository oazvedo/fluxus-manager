using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class AdministradorPlataformaRepository(AppDbContext context)
    : RepositoryBase<AdministradorPlataforma>(context), IAdministradorPlataformaRepository
{
    public Task<bool> EhAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => Set.AnyAsync(a => a.UsuarioId == usuarioId
            && Context.Set<Usuario>().Any(u => u.Id == usuarioId && u.Ativo), cancellationToken);

    public Task<AdministradorPlataforma?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(a => a.UsuarioId == usuarioId, cancellationToken);
}
