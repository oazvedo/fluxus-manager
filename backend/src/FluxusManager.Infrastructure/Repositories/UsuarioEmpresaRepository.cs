using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class UsuarioEmpresaRepository(AppDbContext context) : RepositoryBase<UsuarioEmpresa>(context), IUsuarioEmpresaRepository
{
    public Task<UsuarioEmpresa?> ObterVinculoAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
        // Login, refresh e switch-tenant autenticam antes de haver um claim tenant confiável.
        // O chamador valida a empresa do perfil; a FK composta também impede vínculos cruzados.
        => Set.IgnoreQueryFilters([AppDbContext.TenantFilter]).Include(e => e.Perfil).ThenInclude(perfil => perfil.Permissoes)
            .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId && e.EmpresaId == empresaId, cancellationToken);

    public async Task<IReadOnlyList<UsuarioEmpresa>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => await Set.IgnoreQueryFilters([AppDbContext.TenantFilter]).AsNoTracking().Include(e => e.Perfil).ThenInclude(perfil => perfil.Permissoes)
            .Where(e => e.UsuarioId == usuarioId).OrderBy(e => e.CriadoEm).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UsuarioEmpresa>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().Include(e => e.Perfil).Where(e => e.EmpresaId == empresaId).OrderBy(e => e.CriadoEm).ToListAsync(cancellationToken);
}
