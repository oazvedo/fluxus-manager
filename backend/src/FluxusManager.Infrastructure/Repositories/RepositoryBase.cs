using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

/// <summary>
/// Implementação base dos repositórios. Repositórios específicos herdam desta classe e só adicionam
/// consultas próprias. Não chama SaveChanges: quem grava é o <see cref="IUnitOfWork"/>.
/// </summary>
public class RepositoryBase<TEntity>(AppDbContext context) : IRepository<TEntity> where TEntity : BaseEntity
{
    public const int MaxPageSize = 100;

    protected AppDbContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual Task<PagedResult<TEntity>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ToPagedResultAsync(Set.AsNoTracking(), page, pageSize, cancellationToken);

    public virtual Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.AnyAsync(e => e.Id == id, cancellationToken);

    public virtual void Add(TEntity entity) => Set.Add(entity);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);

    /// <summary>
    /// Pagina uma consulta (ordenada por data de criação). Use nos repositórios específicos para listagens com filtros.
    /// </summary>
    protected static async Task<PagedResult<TEntity>> ToPagedResultAsync(
        IQueryable<TEntity> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.CriadoEm).ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, page, pageSize, totalCount);
    }
}
