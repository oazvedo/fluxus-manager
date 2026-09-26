using FluxusManager.Application.Interfaces;

namespace FluxusManager.Application.Services;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool HasTenant => TenantId.HasValue;

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant inválido.", nameof(tenantId));

        if (TenantId.HasValue && TenantId != tenantId)
            throw new InvalidOperationException("O tenant desta requisição já foi definido.");

        TenantId = tenantId;
    }
}
