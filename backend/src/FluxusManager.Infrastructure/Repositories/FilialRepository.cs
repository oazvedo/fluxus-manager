using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class FilialRepository(AppDbContext context) : RepositoryBase<Filial>(context), IFilialRepository
{
    public Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default)
        => Set.AnyAsync(e => e.Cnpj == cnpj, cancellationToken);
}
