using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Entities;
using FluxusManager.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.UnitTests.MultiTenant;

/// <summary>Entidade usada só nos testes para exercitar as regras multi-tenant do AppDbContext.</summary>
public class Registro : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Nome { get; set; } = string.Empty;

    public Registro(string nome, Guid tenantId = default)
    {
        Nome = nome;
        TenantId = tenantId;
    }
}

/// <summary>AppDbContext com a entidade de teste registrada.</summary>
public class TenantTestDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : AppDbContext(options, tenantContext)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registro>();
        base.OnModelCreating(modelBuilder);
        // O SQLite não tem o xmin do PostgreSQL: a coluna vira um valor fixo (sem detecção de concorrência aqui).
        modelBuilder.Entity<Convite>().Property<uint>("Versao").HasDefaultValue(0u);
    }
}

/// <summary>Banco SQLite em memória compartilhado por vários contextos (um por "requisição").</summary>
public sealed class TenantTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TenantTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext(tenantId: null);
        context.Database.EnsureCreated();
    }

    public TenantTestDbContext CreateContext(Guid? tenantId)
    {
        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
            tenantContext.SetTenant(tenantId.Value);

        return CreateContext(tenantContext);
    }

    /// <summary>Contexto que compartilha o <paramref name="tenantContext"/> com o service testado (que pode definir o tenant).</summary>
    public TenantTestDbContext CreateContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        return new TenantTestDbContext(options, tenantContext);
    }

    public void Dispose() => _connection.Dispose();
}
