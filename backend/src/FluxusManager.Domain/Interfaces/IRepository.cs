using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

/// <summary>
/// Operações básicas de persistência. As alterações só são gravadas no <see cref="IUnitOfWork.CommitAsync"/>.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
