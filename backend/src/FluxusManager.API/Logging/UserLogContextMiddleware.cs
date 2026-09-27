using System.Security.Claims;
using FluxusManager.Application.Interfaces;
using Serilog.Context;

namespace FluxusManager.API.Logging;

/// <summary>
/// Inclui o tenant e o usuário autenticado em todos os logs da requisição.
/// Fica depois da autenticação no pipeline, quando as claims já estão disponíveis.
/// </summary>
public class UserLogContextMiddleware(RequestDelegate next)
{
    public const string TenantProperty = "TenantId";
    public const string UserProperty = "UserId";

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty(TenantProperty, TenantId(context.User)))
        using (LogContext.PushProperty(UserProperty, UserId(context.User)))
            await next(context);
    }

    public static string? TenantId(ClaimsPrincipal user)
        => user.FindFirst(ITenantContext.ClaimType)?.Value;

    // "sub" do JWT; o handler do ASP.NET costuma mapeá-lo para NameIdentifier.
    public static string? UserId(ClaimsPrincipal user)
        => user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
