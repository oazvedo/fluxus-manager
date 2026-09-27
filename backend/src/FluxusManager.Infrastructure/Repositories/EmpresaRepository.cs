using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class EmpresaRepository(AppDbContext context) : RepositoryBase<Empresa>(context), IEmpresaRepository
{
    public Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default)
        => Set.AnyAsync(e => e.Cnpj == cnpj, cancellationToken);
}
