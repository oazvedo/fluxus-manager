using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;

namespace FluxusManager.UnitTests.Auditoria;

public class AuditContextTests
{
    [Fact]
    public void TenantId_VemDoTenantContext()
    {
        var tenantContext = new TenantContext();
        var tenantId = Guid.CreateVersion7();
        tenantContext.SetTenant(tenantId);

        Assert.Equal(tenantId, new AuditContext(tenantContext).TenantId);
    }

    [Fact]
    public void FrontendUrl_MuitoLonga_EhCortadaNoLimite()
    {
        var context = new AuditContext(new TenantContext());

        context.SetFrontendUrl("/" + new string('a', IAuditContext.FrontendUrlMaxLength * 2));

        Assert.Equal(IAuditContext.FrontendUrlMaxLength, context.FrontendUrl!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValoresEmBranco_FicamNulos(string valor)
    {
        var context = new AuditContext(new TenantContext());

        context.SetUser(valor);
        context.SetFrontendUrl(valor);

        Assert.Null(context.User);
        Assert.Null(context.FrontendUrl);
    }
}
