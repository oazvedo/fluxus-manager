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

    /// <summary>Nome do filtro global que esconde registros excluídos logicamente.</summary>
    public const string SoftDeleteFilter = "SoftDelete";

    // Membro do DbContext: o EF reavalia a cada consulta, usando o tenant da requisição atual.
    private Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapeamentos de cada entidade ficam em DatabaseConfigs/ (IEntityTypeConfiguration<T>)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilters(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
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

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        var applyFilter = typeof(AppDbContext).GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

        var entities = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.BaseType is null && typeof(BaseEntity).IsAssignableFrom(t.ClrType));

        foreach (var entityType in entities)
            applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(null, [modelBuilder]);
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
        => modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilter, e => !e.Excluido);

    private void ApplyConventions()
    {
        var now = DateTime.UtcNow;

        // ToList: a exclusão lógica muda o estado das entradas durante o laço.
        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is ITenantEntity)
                ApplyTenant(entry);

            if (entry.Entity is BaseEntity)
            {
                ApplyTimestamps(entry, now);
                ApplySoftDelete(entry, now);
            }
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
                {
                    // Update() marca todas as colunas como alteradas; só é erro se o valor realmente mudou.
                    if (!Equals(tenantId.CurrentValue, tenantId.OriginalValue))
                        throw new InvalidOperationException("O tenant de um registro não pode ser alterado.");

                    tenantId.IsModified = false;
                }

                if ((Guid)tenantId.OriginalValue! != CurrentTenantId)
                    throw new InvalidOperationException("Não é permitido alterar registros de outro tenant.");
                break;
        }
    }

    /// <summary>
    /// Troca o DELETE por um UPDATE de <c>excluido</c> (o trigger de auditoria registra como UPDATE).
    /// ExecuteDelete e SQL direto não passam por aqui e apagam de fato.
    /// </summary>
    private static void ApplySoftDelete(EntityEntry entry, DateTime now)
    {
        if (entry.State != EntityState.Deleted)
            return;

        entry.State = EntityState.Unchanged;
        MarkModified(entry, nameof(BaseEntity.Excluido), true);
        MarkModified(entry, nameof(BaseEntity.AtualizadoEm), now);
    }

    private static void MarkModified(EntityEntry entry, string property, object value)
    {
        entry.Property(property).CurrentValue = value;
        entry.Property(property).IsModified = true;
    }

    private static void ApplyTimestamps(EntityEntry entry, DateTime now)
    {
        if (entry.State == EntityState.Added)
            entry.Property(nameof(BaseEntity.CriadoEm)).CurrentValue = now;
        else if (entry.State == EntityState.Modified)
            entry.Property(nameof(BaseEntity.AtualizadoEm)).CurrentValue = now;
    }
}
