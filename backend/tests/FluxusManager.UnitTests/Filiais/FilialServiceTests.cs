using FluxusManager.Application.Options;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.UnitTests.MultiTenant;
using Microsoft.Extensions.Options;

namespace FluxusManager.UnitTests.Filiais;

public sealed class FilialServiceTests : IDisposable
{
    private readonly TenantTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Criar_ComRaizDiferente_FalhaQuandoValidacaoHabilitada()
    {
        var (service, context) = await CriarServiceAsync(validarRaiz: true);
        await using (context)
        {
            var erro = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                service.CriarAsync(new("Unidade de teste", "12ABC34501DE35", "Rua de teste")));

            Assert.Equal("O CNPJ da filial deve ter a mesma raiz de CNPJ da empresa.", erro.Message);
        }
    }

    [Fact]
    public async Task Criar_ComRaizDiferente_AceitaQuandoValidacaoDesabilitada()
    {
        var (service, context) = await CriarServiceAsync(validarRaiz: false);
        await using (context)
        {
            var filial = await service.CriarAsync(new("Unidade de teste", "12ABC34501DE35", "Rua de teste"));

            Assert.Equal("12ABC34501DE35", filial.Cnpj);
        }
    }

    private async Task<(FilialService service, AppDbContext context)> CriarServiceAsync(bool validarRaiz)
    {
        var empresa = new Empresa("Fluxus Ltda", null, "11222333000181");
        await using (var context = _database.CreateContext(tenantId: null))
        {
            new EmpresaRepository(context).Add(empresa);
            await context.SaveChangesAsync();
        }

        var tenantContext = new TenantContext();
        tenantContext.SetTenant(empresa.Id);
        var tenantDb = _database.CreateContext(empresa.Id);
        var service = new FilialService(
            new FilialRepository(tenantDb),
            new EmpresaRepository(tenantDb),
            tenantContext,
            new UnitOfWork(tenantDb, new AuditContext(tenantContext)),
            Options.Create(new FilialOptions { ValidarRaizCnpjDaEmpresa = validarRaiz }));

        return (service, tenantDb);
    }
}
