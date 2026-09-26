using System.Reflection;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FluxusManager.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    /// <summary>Nome do filtro global por tenant (para usar em IgnoreQueryFilters([TenantFilter])).</summary>
    public const string TenantFilter = "Tenant";

    // Membro do DbContext: o EF reavalia a cada consulta, usando o tenant da requisição atual.
    private Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapeamentos de cada entidade ficam em DatabaseConfigs/ (IEntityTypeConfiguration<T>)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyConventions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyConventions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        var applyFilter = typeof(AppDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        var tenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.BaseType is null && typeof(ITenantEntity).IsAssignableFrom(t.ClrType));

        foreach (var entityType in tenantEntities)
            applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
        => modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilter, e => e.TenantId == CurrentTenantId);

    private void ApplyConventions()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ITenantEntity)
                ApplyTenant(entry);

            if (entry.Entity is BaseEntity)
                ApplyTimestamps(entry, now);
        }
    }

    private void ApplyTenant(EntityEntry entry)
    {
        var tenantId = entry.Property(nameof(ITenantEntity.TenantId));

        switch (entry.State)
        {
            case EntityState.Added:
                var current = CurrentTenantId
                    ?? throw new InvalidOperationException("Não é possível incluir um registro multi-tenant sem tenant na requisição.");

                if ((Guid)tenantId.CurrentValue! == Guid.Empty)
                    tenantId.CurrentValue = current;
                else if ((Guid)tenantId.CurrentValue! != current)
                    throw new InvalidOperationException("Não é permitido incluir registros em outro tenant.");
                break;

            case EntityState.Modified or EntityState.Deleted:
                if (tenantId.IsModified)
                    throw new InvalidOperationException("O tenant de um registro não pode ser alterado.");

                if ((Guid)tenantId.OriginalValue! != CurrentTenantId)
                    throw new InvalidOperationException("Não é permitido alterar registros de outro tenant.");
                break;
        }
    }

    private static void ApplyTimestamps(EntityEntry entry, DateTime now)
    {
        if (entry.State == EntityState.Added)
            entry.Property(nameof(BaseEntity.CriadoEm)).CurrentValue = now;
        else if (entry.State == EntityState.Modified)
            entry.Property(nameof(BaseEntity.AtualizadoEm)).CurrentValue = now;
    }
}
