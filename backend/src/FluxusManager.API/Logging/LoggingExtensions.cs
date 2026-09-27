using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace FluxusManager.API.Logging;

/// <summary>
/// Logs estruturados com Serilog: console legível em Development e JSON (uma linha por evento) nos demais ambientes.
/// Níveis mínimos vêm da seção "Serilog" do appsettings.
/// </summary>
public static class LoggingExtensions
{
    private const string DevTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "FluxusManager.API");

            if (development)
                logger.WriteTo.Console(outputTemplate: DevTemplate);
            else
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        }, preserveStaticLogger: true);

        return builder;
    }

    /// <summary>
    /// Correlation id + um log por requisição (método, caminho, status e duração; nunca corpo ou query string).
    /// Chame antes do roteamento, para que o correlation id cubra toda a requisição.
    /// </summary>
    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            // Logger do container, não o Log.Logger estático (que fica sem uso).
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} em {Elapsed:0.0} ms";
            options.GetLevel = RequestLevel;
            options.EnrichDiagnosticContext = (diagnostic, context) =>
            {
                diagnostic.Set(UserLogContextMiddleware.TenantProperty, UserLogContextMiddleware.TenantId(context.User));
                diagnostic.Set(UserLogContextMiddleware.UserProperty, UserLogContextMiddleware.UserId(context.User));
            };
        });

        return app;
    }

    /// <summary>Tenant e usuário em todos os logs. Chame depois da autenticação.</summary>
    public static WebApplication UseUserLogContext(this WebApplication app)
    {
        app.UseMiddleware<UserLogContextMiddleware>();
        return app;
    }

    // Health check é chamado o tempo todo pelo Docker e pelo deploy: só aparece se falhar.
    private static LogEventLevel RequestLevel(HttpContext context, double elapsed, Exception? exception)
        => exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
            : context.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
            : LogEventLevel.Information;
}
