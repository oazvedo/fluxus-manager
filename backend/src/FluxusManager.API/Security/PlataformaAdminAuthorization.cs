using System.Security.Claims;
using FluxusManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FluxusManager.API.Security;

/// <summary>
/// Papel global do Fluxus. Não vem de claim nem de perfil de empresa: o handler consulta o banco a cada requisição,
/// então revogar tem efeito imediato e nenhuma permissão de tenant (ex.: <c>empresas.editar</c>) substitui o papel.
/// </summary>
public sealed class PlataformaAdminRequirement : IAuthorizationRequirement
{
    public const string Policy = "PlataformaAdmin";
}

public sealed class PlataformaAdminHandler(IAdministradorPlataformaRepository administradores)
    : AuthorizationHandler<PlataformaAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlataformaAdminRequirement requirement)
    {
        var subject = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(subject, out var usuarioId) && await administradores.EhAdministradorAsync(usuarioId))
            context.Succeed(requirement);
    }
}

public static class PlataformaAdminExtensions
{
    public static IServiceCollection AddAutorizacaoPlataforma(this IServiceCollection services)
    {
        // Scoped: usa o repositório (AppDbContext) da requisição.
        services.AddScoped<IAuthorizationHandler, PlataformaAdminHandler>();
        services.AddAuthorizationBuilder().AddPolicy(PlataformaAdminRequirement.Policy, policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PlataformaAdminRequirement()));
        return services;
    }

    /// <summary>Id do usuário autenticado (claim <c>sub</c>).</summary>
    public static Guid UsuarioId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Usuário autenticado sem identificador."));
}
