using FluxusManager.Application.Services;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.UnitTests.MultiTenant;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.UnitTests.Persistence;

public sealed class SoftDeleteTests : IDisposable
{
    private static readonly Guid Tenant = Guid.CreateVersion7();

    private readonly TenantTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<Registro> IncluirERemoverAsync()
    {
        var registro = new Registro("a excluir");
        await using (var context = _database.CreateContext(Tenant))
        {
            context.Add(registro);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext(Tenant))
        {
            var repository = new RepositoryBase<Registro>(context);
            repository.Remove((await repository.GetByIdAsync(registro.Id))!);
            await new UnitOfWork(context, new AuditContext(new TenantContext())).CommitAsync();
        }

        return registro;
    }

    [Fact]
    public async Task Remove_NaoApagaALinha_MarcaComoExcluido()
    {
        var registro = await IncluirERemoverAsync();

        await using var context = _database.CreateContext(Tenant);
        var excluido = await context.Set<Registro>()
            .IgnoreQueryFilters([AppDbContext.SoftDeleteFilter])
            .SingleAsync(r => r.Id == registro.Id);

        Assert.True(excluido.Excluido);
        Assert.NotNull(excluido.AtualizadoEm);
    }

    [Fact]
    public async Task Excluido_NaoApareceNasConsultas()
    {
        var registro = await IncluirERemoverAsync();

        await using var context = _database.CreateContext(Tenant);
        var repository = new RepositoryBase<Registro>(context);

        Assert.Null(await repository.GetByIdAsync(registro.Id));
        Assert.Equal(0, (await repository.ListAsync(1, 10)).TotalCount);
    }

    [Fact]
    public async Task IgnorarSoOFiltroDeExclusao_MantemOIsolamentoPorTenant()
    {
        await IncluirERemoverAsync();

        await using var outroTenant = _database.CreateContext(Guid.CreateVersion7());

        Assert.Empty(await outroTenant.Set<Registro>().IgnoreQueryFilters([AppDbContext.SoftDeleteFilter]).ToListAsync());
    }
}
