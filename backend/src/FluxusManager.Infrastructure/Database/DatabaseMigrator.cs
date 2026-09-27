using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluxusManager.Infrastructure.Database;

public static class DatabaseMigrator
{
    /// <summary>
    /// Aplica as migrations pendentes na subida da API (Database:MigrateOnStartup, ligado por padrão).
    /// Seguro para uma instância da API por banco, que é o nosso cenário.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));

        var pendentes = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendentes.Count == 0)
        {
            logger.LogInformation("Banco de dados atualizado: nenhuma migration pendente.");
            return;
        }

        logger.LogInformation("Aplicando {Quantidade} migration(s): {Migrations}", pendentes.Count, string.Join(", ", pendentes));
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations aplicadas.");
    }
}
