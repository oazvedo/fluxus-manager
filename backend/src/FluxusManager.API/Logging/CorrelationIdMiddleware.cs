using System.Text.RegularExpressions;
using Serilog.Context;

namespace FluxusManager.API.Logging;

/// <summary>
/// Garante um correlation id por requisição: usa o header <c>X-Correlation-Id</c> recebido (se for válido)
/// ou gera um novo, devolve no mesmo header da resposta e o inclui em todos os logs da requisição.
/// </summary>
public partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogProperty = "CorrelationId";

    private const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var received = context.Request.Headers[HeaderName].ToString();
        var correlationId = ValidId().IsMatch(received) ? received : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogProperty, correlationId))
            await next(context);
    }

    /// <summary>Correlation id da requisição atual (vazio fora do pipeline).</summary>
    public static string Get(HttpContext context)
        => context.Items[ItemKey] as string ?? string.Empty;

    // Valor vem do cliente e vai para os logs: só aceita um identificador curto, sem espaços ou quebras de linha.
    [GeneratedRegex("^[A-Za-z0-9._:-]{1,64}$")]
    private static partial Regex ValidId();
}
