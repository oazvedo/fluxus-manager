using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.IntegrationTests;

/// <summary>
/// Sobe a API em memória, incluindo os controllers de teste deste assembly.
/// Não acessa o banco: a connection string só precisa existir para o InfraModule.
/// </summary>
public class ApiFactory(string environment) : WebApplicationFactory<Program>
{
    public ApiFactory() : this("Development")
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=fluxus_tests");
        // Nos testes não há PostgreSQL; o banco (quando usado) é criado pelo ApiComBancoFactory.
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly));
    }
}
