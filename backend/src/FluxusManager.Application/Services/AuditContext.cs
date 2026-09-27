using FluxusManager.Application.Interfaces;

namespace FluxusManager.Application.Services;

public class AuditContext(ITenantContext tenantContext) : IAuditContext
{
    public string? User { get; private set; }

    public Guid? TenantId => tenantContext.TenantId;

    public string? FrontendUrl { get; private set; }

    public void SetUser(string user)
        => User = string.IsNullOrWhiteSpace(user) ? null : user.Trim();

    public void SetFrontendUrl(string frontendUrl)
    {
        var url = frontendUrl.Trim();
        FrontendUrl = url.Length == 0 ? null : url[..Math.Min(url.Length, IAuditContext.FrontendUrlMaxLength)];
    }
}
