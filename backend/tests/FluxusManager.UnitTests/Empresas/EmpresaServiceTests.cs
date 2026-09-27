using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.UnitTests.MultiTenant;

namespace FluxusManager.UnitTests.Empresas;

public sealed class EmpresaServiceTests : IDisposable
{
    // Banco SQLite em memória; empresa é global (é o próprio tenant), então não precisa de tenant.
    private readonly TenantTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private (EmpresaService service, AppDbContext context) CriarService()
    {
        var context = _database.CreateContext(tenantId: null);
        var service = new EmpresaService(new EmpresaRepository(context), new UnitOfWork(context, new AuditContext(new TenantContext())));
        return (service, context);
    }

    private async Task<EmpresaResponse> CriarEmpresaAsync(string cnpj = "11222333000181")
    {
        var (service, context) = CriarService();
        await using (context)
            return await service.CriarAsync(new CriarEmpresaRequest("Fluxus Ltda", "Fluxus", cnpj));
    }

    [Fact]
    public async Task Criar_NormalizaOsCampos_ENasceAtiva()
    {
        var (service, context) = CriarService();
        await using var _ = context;

        var empresa = await service.CriarAsync(new CriarEmpresaRequest("  Fluxus Ltda ", "   ", "12.abc.345/01de-35"));

        Assert.Equal("Fluxus Ltda", empresa.RazaoSocial);
        Assert.Null(empresa.NomeFantasia);
        Assert.Equal("12ABC34501DE35", empresa.Cnpj);
        Assert.True(empresa.Ativo);
    }

    [Fact]
    public async Task Criar_ComCnpjJaCadastrado_EmOutroFormato_Falha()
    {
        await CriarEmpresaAsync("11222333000181");
        var (service, context) = CriarService();
        await using var _ = context;

        var erro = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CriarAsync(new CriarEmpresaRequest("Outra", null, "11.222.333/0001-81")));

        Assert.Equal("Já existe uma empresa com o CNPJ 11.222.333/0001-81.", erro.Message);
    }

    [Fact]
    public async Task Atualizar_AlteraNomes_EMantemOCnpj()
    {
        var criada = await CriarEmpresaAsync();
        var (service, context) = CriarService();
        await using var _ = context;

        var empresa = await service.AtualizarAsync(criada.Id, new AtualizarEmpresaRequest("Fluxus S.A.", "Fluxus Manager"));

        Assert.Equal("Fluxus S.A.", empresa.RazaoSocial);
        Assert.Equal("Fluxus Manager", empresa.NomeFantasia);
        Assert.Equal(criada.Cnpj, empresa.Cnpj);
    }

    [Fact]
    public async Task InativarEAtivar_AlteramOStatus()
    {
        var criada = await CriarEmpresaAsync();

        var (service, context) = CriarService();
        await using (context)
            await service.InativarAsync(criada.Id);

        (service, context) = CriarService();
        await using (context)
            Assert.False((await service.ObterAsync(criada.Id)).Ativo);

        (service, context) = CriarService();
        await using (context)
        {
            await service.AtivarAsync(criada.Id);
            Assert.True((await service.ObterAsync(criada.Id)).Ativo);
        }
    }

    [Fact]
    public async Task Obter_Inexistente_Falha()
    {
        var (service, context) = CriarService();
        await using var _ = context;

        await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(Guid.CreateVersion7()));
    }
}
