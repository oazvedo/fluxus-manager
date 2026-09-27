using Microsoft.AspNetCore.Hosting;

namespace FluxusManager.IntegrationTests;

/// <summary>
/// API em memória ligada a um banco PostgreSQL próprio (ver <see cref="Database.PostgresFixture"/>).
/// As migrations rodam na subida, como em produção.
/// </summary>
public class ApiComBancoFactory(string connectionString, bool useTestAuthentication = true, bool useRealAuthService = false)
    : ApiFactory("Development", useTestAuthentication, useRealAuthService)
{
    public string ConnectionString { get; } = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }
}
