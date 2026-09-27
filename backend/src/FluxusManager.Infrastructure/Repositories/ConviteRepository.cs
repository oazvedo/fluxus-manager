using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class ConviteRepository(AppDbContext context) : RepositoryBase<Convite>(context), IConviteRepository
{
    public override Task<PagedResult<Convite>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ToPagedResultAsync(Set.AsNoTracking().Include(c => c.Perfil), page, pageSize, cancellationToken);

    public Task<Convite?> ObterComPerfilAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.Include(c => c.Perfil).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Convite?> ObterPendentePorEmailAsync(string email, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(c => c.Email == email && c.Status == ConviteStatus.Pendente, cancellationToken);

    public Task<Convite?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Set.IgnoreQueryFilters([AppDbContext.TenantFilter]).FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);
}
