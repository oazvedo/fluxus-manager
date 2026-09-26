using FluxusManager.Application.Services;

namespace FluxusManager.UnitTests.MultiTenant;

public class TenantContextTests
{
    [Fact]
    public void Inicialmente_NaoTemTenant()
    {
        var context = new TenantContext();

        Assert.False(context.HasTenant);
        Assert.Null(context.TenantId);
    }

    [Fact]
    public void SetTenant_DefineOTenant()
    {
        var context = new TenantContext();
        var tenantId = Guid.CreateVersion7();

        context.SetTenant(tenantId);

        Assert.True(context.HasTenant);
        Assert.Equal(tenantId, context.TenantId);
    }

    [Fact]
    public void SetTenant_ComGuidVazio_Falha()
    {
        Assert.Throws<ArgumentException>(() => new TenantContext().SetTenant(Guid.Empty));
    }

    [Fact]
    public void SetTenant_TrocandoOTenantNaMesmaRequisicao_Falha()
    {
        var context = new TenantContext();
        context.SetTenant(Guid.CreateVersion7());

        Assert.Throws<InvalidOperationException>(() => context.SetTenant(Guid.CreateVersion7()));
    }

    [Fact]
    public void SetTenant_ComOMesmoTenant_EhIdempotente()
    {
        var context = new TenantContext();
        var tenantId = Guid.CreateVersion7();

        context.SetTenant(tenantId);
        context.SetTenant(tenantId);

        Assert.Equal(tenantId, context.TenantId);
    }
}
