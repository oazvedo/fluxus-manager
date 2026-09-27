using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Database;

/// <summary>
/// Toda gravação roda numa transação explícita que começa passando o <see cref="IAuditContext"/> ao PostgreSQL
/// (<c>set_config(..., true)</c>, equivalente a <c>SET LOCAL</c>). O trigger de auditoria lê esses valores.
/// </summary>
public class UnitOfWork(AppDbContext context, IAuditContext auditContext) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        // Já dentro de ExecuteInTransactionAsync (contexto aplicado) ou sem nada a gravar: não abre outra transação.
        if (context.Database.CurrentTransaction is not null || !context.ChangeTracker.HasChanges())
            return context.SaveChangesAsync(cancellationToken);

        return InTransactionAsync(context.SaveChangesAsync, cancellationToken);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(async ct =>
        {
            await action(ct);
            return true;
        }, cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        => InTransactionAsync(async ct =>
        {
            var result = await action(ct);
            await context.SaveChangesAsync(ct);
            return result;
        }, cancellationToken);

    private Task<TResult> InTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken)
    {
        // Transação explícita via execution strategy: exigido pelo EF caso o retry de conexão seja habilitado.
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            await ApplyAuditContextAsync(ct);
            var result = await work(ct);
            await transaction.CommitAsync(ct);

            return result;
        }, cancellationToken);
    }

    private async Task ApplyAuditContextAsync(CancellationToken cancellationToken)
    {
        // A auditoria é do PostgreSQL; o SQLite dos testes unitários não tem set_config.
        if (!context.Database.IsNpgsql())
            return;

        await context.Database.ExecuteSqlAsync($"""
            SELECT set_config('app.current_user', {auditContext.User ?? ""}, true),
                   set_config('app.tenant_id', {auditContext.TenantId?.ToString() ?? ""}, true),
                   set_config('app.frontend_url', {auditContext.FrontendUrl ?? ""}, true)
            """, cancellationToken);
    }
}
