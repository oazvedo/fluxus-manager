using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.Infrastructure.Database;

public static class DatabaseMigrator
{
    /// <summary>
    /// Aplica as migrations pendentes. Usado no deploy (Database:MigrateOnStartup=true),
    /// onde há uma única instância da API por ambiente.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
