using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FluxusManager.IntegrationTests.Auth;

[Collection(PostgresCollection.Name)]
public sealed class DevelopmentSeedTests(PostgresFixture postgres)
{
    private const string Password = "senha-de-teste-59";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Seed_ConflitoNaoPromoveUsuarioNemGravaDadosParciais(bool emailConflict)
    {
        await using var factory = new SeedFactory(await postgres.CreateDatabaseAsync(), enabled: false);
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (emailConflict)
                db.Add(new Usuario("Existente", DevelopmentSeeder.AdminEmail, "hash-existente"));
            else
                db.Add(new Empresa("Existente", null, DevelopmentSeeder.EmpresaCnpj));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }
        factory.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.Services.SeedDevelopmentAsync());
        await using var verification = factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await context.Set<Usuario>().AnyAsync(u => u.Id == DevelopmentSeeder.AdminId));
        Assert.Empty(await context.Set<UsuarioEmpresa>().ToListAsync());
        Assert.Equal(emailConflict ? 1 : 0, await context.Set<Usuario>().CountAsync());
        Assert.Equal(emailConflict ? 0 : 1, await context.Set<Empresa>().CountAsync());
    }

    [Fact]
    public async Task Seed_CriaAdminComTodasPermissoes_EPreservaAlteracoesAoReiniciar()
    {
        await using var factory = new SeedFactory(await postgres.CreateDatabaseAsync());
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(DevelopmentSeeder.AdminEmail, Password));
        response.EnsureSuccessStatusCode();
        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        Assert.Equal(PermissionCatalog.All.Order(), tokens.Permissions.Order());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        foreach (var route in new[] { "/empresas", "/usuarios", "/filiais" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);

        await factory.Services.SeedDevelopmentAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Set<Usuario>().SingleAsync();
            Assert.Equal(DevelopmentSeeder.AdminId, admin.Id);
            Assert.NotEqual(Password, admin.SenhaHash);
            Assert.Single(await db.Set<Empresa>().ToListAsync());
            var link = await db.Set<UsuarioEmpresa>().SingleAsync();
            admin.Atualizar("Nome alterado", "alterado@fluxus.local");
            admin.Inativar();
            db.Remove(link);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
            var audit = await db.Database.SqlQuery<string>($"""
                SELECT coalesce(new_values::text, '') AS "Value" FROM audit.change_log WHERE table_name = 'usuarios'
                """).ToListAsync();
            Assert.NotEmpty(audit);
            Assert.All(audit, entry =>
            {
                Assert.DoesNotContain(Password, entry);
                Assert.DoesNotContain(admin.SenhaHash, entry);
            });
        }
        await factory.Services.SeedDevelopmentAsync();
        await using var verification = factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var preserved = await context.Set<Usuario>().SingleAsync();
        Assert.Equal("Nome alterado", preserved.Nome);
        Assert.False(preserved.Ativo);
        Assert.Empty(await context.Set<UsuarioEmpresa>().ToListAsync());
    }

    [Theory]
    [InlineData("Production", true, Password)]
    [InlineData("Development", false, Password)]
    [InlineData("Development", true, "")]
    public async Task Seed_SoExecutaEmDevelopmentHabilitadoComSenha(string environment, bool enabled, string password)
    {
        await using var factory = new SeedFactory(await postgres.CreateDatabaseAsync(), environment, enabled, password);
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Usuario>().ToListAsync());
    }

    [Fact]
    public async Task Seed_NaoRecriaAdministradorExcluido()
    {
        await using var factory = new SeedFactory(await postgres.CreateDatabaseAsync());
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Remove(await db.Set<Usuario>().SingleAsync());
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }
        await factory.Services.SeedDevelopmentAsync();
        await using var verification = factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await context.Set<Usuario>().ToListAsync());
        Assert.Single(await context.Set<Usuario>().IgnoreQueryFilters().ToListAsync());
    }

    private sealed class SeedFactory(string connectionString, string environment = "Development", bool enabled = true, string password = Password)
        : ApiFactory(environment, useTestAuthentication: false, useRealAuthService: true)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("DevelopmentSeed:Enabled", enabled.ToString());
            builder.UseSetting("DevelopmentSeed:AdminPassword", password);
        }
    }
}
