using System.Security.Claims;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FluxusManager.API.Filters;

/// <summary>
/// Preenche o <see cref="IAuditContext"/> da requisição: usuário (claim <c>sub</c> do JWT) e a tela do
/// frontend (header <c>X-Frontend-Url</c>). O tenant vem do <see cref="TenantFilter"/>.
/// </summary>
public class AuditFilter(IAuditContext auditContext) : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        // "sub" do JWT; o handler do ASP.NET costuma mapeá-lo para NameIdentifier.
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
            auditContext.SetUser(userId);

        var frontendUrl = context.HttpContext.Request.Headers[IAuditContext.FrontendUrlHeader].ToString();
        if (frontendUrl.Length > 0)
            auditContext.SetFrontendUrl(frontendUrl);

        return next();
    }
}
