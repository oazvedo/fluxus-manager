using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public class MigrationsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migrations_AplicamNumBancoVazio_ESemAlteracaoPendenteNoModelo()
    {
        await using var factory = new ApiComBancoFactory(await postgres.CreateDatabaseAsync());
        _ = factory.Server; // sobe a API, que aplica as migrations

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges(),
            "O modelo mudou sem migration. Rode: dotnet ef migrations add <Nome> -p src/FluxusManager.Infrastructure -s src/FluxusManager.API");
    }
}
