using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FluxusManager.API.Security;

/// <summary>Limite por minuto das actions públicas (<c>[AllowAnonymous]</c>) dos controllers.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requisições por minuto de um mesmo IP em cada action pública.</summary>
    public int RequisicoesPorMinuto { get; set; } = 20;
}

public static class RateLimitingExtensions
{
    /// <summary>
    /// Toda action pública de controller (login, convites...) é limitada por IP e endpoint, sem atributo por action:
    /// uma rota pública nova já nasce protegida. Ficam de fora health check, OpenAPI e actions com
    /// <c>[DisableRateLimiting]</c> (refresh e logout: a credencial não é adivinhável, e um 429 ali derrubaria a sessão
    /// de quem compartilha o IP ou deixaria de revogar o token).
    /// </summary>
    public static IServiceCollection AddRateLimitingPublico(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .Validate(o => o.RequisicoesPorMinuto is >= 1 and <= 10_000, "RateLimiting:RequisicoesPorMinuto deve estar entre 1 e 10000.")
            .ValidateOnStart();

        services.AddRateLimiter(options => options.OnRejected = RejeitarAsync);
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitingOptions>>((options, limites) =>
        {
            var janela = new FixedWindowRateLimiterOptions
            {
                PermitLimit = limites.Value.RequisicoesPorMinuto,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            };
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, (IPAddress?, string?)>(context =>
            {
                var metadata = context.GetEndpoint()?.Metadata;
                if (metadata?.GetMetadata<ControllerActionDescriptor>() is not { } action
                    || metadata.GetMetadata<IAllowAnonymous>() is null
                    || metadata.GetMetadata<DisableRateLimitingAttribute>() is not null)
                    return RateLimitPartition.GetNoLimiter<(IPAddress?, string?)>(default);

                // Por action, não pelo caminho: variações de maiúsculas ou barra final caem na mesma contagem.
                return RateLimitPartition.GetFixedWindowLimiter((Origem(context.Connection.RemoteIpAddress), action.DisplayName), _ => janela);
            });
        });
        return services;
    }

    /// <summary>
    /// IPv4 mapeado em IPv6 vira IPv4, e IPv6 é agrupado por /64 (o bloco que um cliente comum recebe):
    /// trocar de endereço dentro do próprio bloco não abre uma contagem nova.
    /// </summary>
    private static IPAddress? Origem(IPAddress? ip)
    {
        if (ip is null)
            return null;
        if (ip.IsIPv4MappedToIPv6)
            return ip.MapToIPv4();
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            return ip;

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes);
    }

    /// <summary>
    /// Com <c>Proxy:Confiavel=true</c>, o IP do cliente vem do <c>X-Forwarded-For</c> (só o último salto, o do nginx).
    /// Ligue apenas com a API atrás do proxy: sem ele, o cliente poderia forjar o cabeçalho.
    /// </summary>
    public static IServiceCollection AddProxyConfiavel(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Proxy:Confiavel"))
            return services;

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // O proxy é o container do nginx, com IP dinâmico na rede do Docker; a API não é exposta fora dela.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static async ValueTask RejeitarAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        var segundos = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;
        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        http.Response.Headers.RetryAfter = segundos.ToString();

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas tentativas",
                Detail = $"Muitas tentativas em pouco tempo. Aguarde {segundos} segundos e tente novamente.",
                Instance = http.Request.Path
            }
        });
    }
}
