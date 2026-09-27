using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Entities;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.IntegrationTests.MultiTenant;

/// <summary>
/// Isolamento entre tenants no PostgreSQL: o filtro global do AppDbContext precisa virar SQL
/// que o Npgsql executa, não só funcionar no SQLite dos testes unitários.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TenantIsolationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync();

        await using var context = CreateContext(tenantId: null);
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private IsolamentoDbContext CreateContext(Guid? tenantId)
    {
        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
            tenantContext.SetTenant(tenantId.Value);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IsolamentoDbContext(options, tenantContext);
    }

    private async Task IncluirAsync(Guid tenantId, string nome)
    {
        await using var context = CreateContext(tenantId);
        context.Add(new RegistroIsolado(nome));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Consulta_RetornaSomenteOsRegistrosDoTenant()
    {
        await IncluirAsync(TenantA, "do A");
        await IncluirAsync(TenantB, "do B");

        await using var contextA = CreateContext(TenantA);
        await using var contextB = CreateContext(TenantB);

        Assert.Equal(["do A"], await contextA.Set<RegistroIsolado>().Select(r => r.Nome).ToListAsync());
        Assert.Equal(["do B"], await contextB.Set<RegistroIsolado>().Select(r => r.Nome).ToListAsync());
    }

    [Fact]
    public async Task BuscaPorId_DeOutroTenant_NaoEncontra()
    {
        await IncluirAsync(TenantB, "do B");

        await using var contextB = CreateContext(TenantB);
        var id = await contextB.Set<RegistroIsolado>().Select(r => r.Id).SingleAsync();

        await using var contextA = CreateContext(TenantA);
        Assert.Null(await contextA.Set<RegistroIsolado>().FirstOrDefaultAsync(r => r.Id == id));
    }

    [Fact]
    public async Task Consulta_SemTenant_NaoRetornaNada()
    {
        await IncluirAsync(TenantA, "do A");

        await using var context = CreateContext(tenantId: null);

        Assert.Empty(await context.Set<RegistroIsolado>().ToListAsync());
    }

    [Fact]
    public async Task ExclusaoEmLote_AfetaSomenteOTenant()
    {
        await IncluirAsync(TenantA, "do A");
        await IncluirAsync(TenantB, "do B");

        await using (var contextA = CreateContext(TenantA))
            Assert.Equal(1, await contextA.Set<RegistroIsolado>().ExecuteDeleteAsync());

        await using var contextB = CreateContext(TenantB);
        Assert.Equal(1, await contextB.Set<RegistroIsolado>().CountAsync());
    }
}

/// <summary>Entidade multi-tenant usada só neste teste.</summary>
public class RegistroIsolado(string nome) : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Nome { get; private set; } = nome;
}

public class IsolamentoDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : AppDbContext(options, tenantContext)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroIsolado>().ToTable("registros_isolados");
        base.OnModelCreating(modelBuilder);
    }
}
