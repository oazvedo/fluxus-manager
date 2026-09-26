namespace FluxusManager.Domain.Interfaces;

/// <summary>
/// Confirma as alterações feitas pelos repositórios. Services orquestram e chamam o commit;
/// repositórios nunca gravam sozinhos.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Grava todas as alterações pendentes numa única transação.</summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa <paramref name="action"/> dentro de uma transação explícita e grava as alterações ao final.
    /// Se a ação lançar exceção, nada é gravado.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);
}
