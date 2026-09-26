using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.UnitTests.MultiTenant;

public sealed class AppDbContextTenantTests : IDisposable
{
    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();

    private readonly TenantTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<Registro> IncluirAsync(Guid tenantId, string nome)
    {
        await using var context = _database.CreateContext(tenantId);
        var registro = new Registro(nome);
        context.Add(registro);
        await context.SaveChangesAsync();
        return registro;
    }

    [Fact]
    public async Task Consulta_RetornaSomenteRegistrosDoTenantAtual()
    {
        await IncluirAsync(TenantA, "do A");
        await IncluirAsync(TenantB, "do B");

        await using var context = _database.CreateContext(TenantA);
        var registros = await context.Set<Registro>().ToListAsync();

        var registro = Assert.Single(registros);
        Assert.Equal("do A", registro.Nome);
        Assert.Equal(TenantA, registro.TenantId);
    }

    [Fact]
    public async Task Consulta_SemTenant_NaoRetornaNada()
    {
        await IncluirAsync(TenantA, "do A");

        await using var context = _database.CreateContext(tenantId: null);

        Assert.Empty(await context.Set<Registro>().ToListAsync());
    }

    [Fact]
    public async Task Consulta_IgnorandoFiltroDeTenant_RetornaTodos()
    {
        await IncluirAsync(TenantA, "do A");
        await IncluirAsync(TenantB, "do B");

        await using var context = _database.CreateContext(TenantA);
        var registros = await context.Set<Registro>().IgnoreQueryFilters([AppDbContext.TenantFilter]).ToListAsync();

        Assert.Equal(2, registros.Count);
    }

    [Fact]
    public async Task Inclusao_PreencheTenantEDataDeCriacao()
    {
        var registro = await IncluirAsync(TenantA, "novo");

        Assert.Equal(TenantA, registro.TenantId);
        Assert.NotEqual(default, registro.CriadoEm);
        Assert.Null(registro.AtualizadoEm);
    }

    [Fact]
    public async Task Inclusao_SemTenant_Falha()
    {
        await using var context = _database.CreateContext(tenantId: null);
        context.Add(new Registro("sem tenant"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Inclusao_EmOutroTenant_Falha()
    {
        await using var context = _database.CreateContext(TenantA);
        context.Add(new Registro("do B", TenantB));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Alteracao_PreencheDataDeAtualizacao()
    {
        var incluido = await IncluirAsync(TenantA, "original");

        await using var context = _database.CreateContext(TenantA);
        var registro = await context.Set<Registro>().SingleAsync(r => r.Id == incluido.Id);
        registro.Nome = "alterado";
        await context.SaveChangesAsync();

        Assert.NotNull(registro.AtualizadoEm);
    }

    [Fact]
    public async Task Alteracao_DoTenantId_Falha()
    {
        var incluido = await IncluirAsync(TenantA, "do A");

        await using var context = _database.CreateContext(TenantA);
        var registro = await context.Set<Registro>().SingleAsync(r => r.Id == incluido.Id);
        context.Entry(registro).Property(r => r.TenantId).CurrentValue = TenantB;

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Alteracao_DeRegistroDeOutroTenant_Falha()
    {
        var doB = await IncluirAsync(TenantB, "do B");

        await using var context = _database.CreateContext(TenantA);
        context.Attach(doB);
        doB.Nome = "invadido";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Exclusao_DeRegistroDeOutroTenant_Falha()
    {
        var doB = await IncluirAsync(TenantB, "do B");

        await using var context = _database.CreateContext(TenantA);
        context.Remove(doB);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}
