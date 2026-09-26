using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.UnitTests.MultiTenant;

namespace FluxusManager.UnitTests.Persistence;

public sealed class RepositoryBaseTests : IDisposable
{
    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();

    private readonly TenantTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<List<Registro>> IncluirAsync(Guid tenantId, params string[] nomes)
    {
        await using var context = _database.CreateContext(tenantId);
        var repository = new RepositoryBase<Registro>(context);
        var registros = nomes.Select(n => new Registro(n)).ToList();
        registros.ForEach(repository.Add);
        await new UnitOfWork(context).CommitAsync();
        return registros;
    }

    [Fact]
    public async Task Add_SemCommit_NaoGrava()
    {
        await using (var context = _database.CreateContext(TenantA))
        {
            new RepositoryBase<Registro>(context).Add(new Registro("pendente"));
        }

        await using var leitura = _database.CreateContext(TenantA);
        var pagina = await new RepositoryBase<Registro>(leitura).ListAsync(1, 10);
        Assert.Equal(0, pagina.TotalCount);
    }

    [Fact]
    public async Task GetById_RetornaRegistroDoTenant()
    {
        var registro = (await IncluirAsync(TenantA, "um"))[0];

        await using var context = _database.CreateContext(TenantA);
        var encontrado = await new RepositoryBase<Registro>(context).GetByIdAsync(registro.Id);

        Assert.NotNull(encontrado);
        Assert.Equal("um", encontrado.Nome);
    }

    [Fact]
    public async Task GetById_DeOutroTenant_RetornaNulo()
    {
        var registro = (await IncluirAsync(TenantB, "do B"))[0];

        await using var context = _database.CreateContext(TenantA);
        var repository = new RepositoryBase<Registro>(context);

        Assert.Null(await repository.GetByIdAsync(registro.Id));
        Assert.False(await repository.ExistsAsync(registro.Id));
    }

    [Fact]
    public async Task List_PaginaERespeitaOTenant()
    {
        await IncluirAsync(TenantA, "a1", "a2", "a3", "a4", "a5");
        await IncluirAsync(TenantB, "b1");

        await using var context = _database.CreateContext(TenantA);
        var repository = new RepositoryBase<Registro>(context);
        var pagina2 = await repository.ListAsync(page: 2, pageSize: 2);

        Assert.Equal(5, pagina2.TotalCount);
        Assert.Equal(3, pagina2.TotalPages);
        Assert.Equal(2, pagina2.Items.Count);
        Assert.True(pagina2.HasNextPage);
        Assert.True(pagina2.HasPreviousPage);
        Assert.All(pagina2.Items, r => Assert.Equal(TenantA, r.TenantId));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, RepositoryBase<Registro>.MaxPageSize + 1)]
    public async Task List_ComPaginacaoInvalida_Falha(int page, int pageSize)
    {
        await using var context = _database.CreateContext(TenantA);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new RepositoryBase<Registro>(context).ListAsync(page, pageSize));
    }

    [Fact]
    public async Task Update_ERemove_GravamNoCommit()
    {
        var registros = await IncluirAsync(TenantA, "alterar", "remover");

        await using (var context = _database.CreateContext(TenantA))
        {
            var repository = new RepositoryBase<Registro>(context);
            var alterar = (await repository.GetByIdAsync(registros[0].Id))!;
            alterar.Nome = "alterado";
            repository.Update(alterar);
            repository.Remove((await repository.GetByIdAsync(registros[1].Id))!);
            await new UnitOfWork(context).CommitAsync();
        }

        await using var leitura = _database.CreateContext(TenantA);
        var pagina = await new RepositoryBase<Registro>(leitura).ListAsync(1, 10);
        var restante = Assert.Single(pagina.Items);
        Assert.Equal("alterado", restante.Nome);
    }

    [Fact]
    public async Task ExecuteInTransaction_GravaAoFinal()
    {
        await using (var context = _database.CreateContext(TenantA))
        {
            var repository = new RepositoryBase<Registro>(context);
            var id = await new UnitOfWork(context).ExecuteInTransactionAsync(ct =>
            {
                var registro = new Registro("na transação");
                repository.Add(registro);
                return Task.FromResult(registro.Id);
            });
            Assert.NotEqual(Guid.Empty, id);
        }

        await using var leitura = _database.CreateContext(TenantA);
        Assert.Equal(1, (await new RepositoryBase<Registro>(leitura).ListAsync(1, 10)).TotalCount);
    }

    [Fact]
    public async Task ExecuteInTransaction_ComErro_NaoGravaNada()
    {
        await using (var context = _database.CreateContext(TenantA))
        {
            var repository = new RepositoryBase<Registro>(context);
            var unitOfWork = new UnitOfWork(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                repository.Add(new Registro("primeiro"));
                await context.SaveChangesAsync(ct); // gravação intermediária dentro da transação
                throw new InvalidOperationException("falhou no meio");
            }));
        }

        await using var leitura = _database.CreateContext(TenantA);
        Assert.Equal(0, (await new RepositoryBase<Registro>(leitura).ListAsync(1, 10)).TotalCount);
    }
}
