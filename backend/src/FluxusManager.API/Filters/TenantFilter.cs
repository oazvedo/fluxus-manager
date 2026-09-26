using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FluxusManager.API.Filters;

/// <summary>
/// Resolve o tenant da requisição a partir da claim <c>tenant_id</c> do JWT.
/// Sem a claim, o tenant fica indefinido: consultas multi-tenant não retornam nada e inclusões falham.
/// </summary>
public class TenantFilter(ITenantContext tenantContext) : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var claim = context.HttpContext.User.FindFirst(ITenantContext.ClaimType)?.Value;

        if (Guid.TryParse(claim, out var tenantId) && tenantId != Guid.Empty)
            tenantContext.SetTenant(tenantId);

        return next();
    }
}
