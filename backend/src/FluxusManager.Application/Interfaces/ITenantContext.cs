namespace FluxusManager.Application.Interfaces;

/// <summary>
/// Tenant (empresa) da requisição atual.
/// </summary>
public interface ITenantContext
{
    /// <summary>Claim do JWT que carrega o tenant ativo.</summary>
    const string ClaimType = "tenant_id";

    Guid? TenantId { get; }

    bool HasTenant { get; }

    void SetTenant(Guid tenantId);
}
