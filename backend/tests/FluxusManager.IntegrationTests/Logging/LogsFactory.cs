using System.Collections.Concurrent;
using System.Security.Claims;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace FluxusManager.IntegrationTests.Logging;

/// <summary>
/// API em memória que guarda os eventos de log para inspeção. Os headers <c>X-Test-Tenant</c> e
/// <c>X-Test-User</c> viram claims do usuário, simulando o login que ainda não existe.
/// </summary>
public class LogsFactory(string environment = "Development") : ApiFactory(environment)
{
    public const string TenantHeader = "X-Test-Tenant";
    public const string UserHeader = "X-Test-User";

    public CollectingSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<IStartupFilter, TestUserStartupFilter>();
        });
    }

    private sealed class TestUserStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                var claims = new List<Claim>();
                if (context.Request.Headers.TryGetValue(TenantHeader, out var tenant))
                    claims.Add(new Claim(ITenantContext.ClaimType, tenant.ToString()));
                if (context.Request.Headers.TryGetValue(UserHeader, out var user))
                    claims.Add(new Claim("sub", user.ToString()));

                if (claims.Count > 0)
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

                return nextMiddleware();
            });
            next(app);
        };
    }
}

public sealed class CollectingSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyCollection<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    public LogEvent Single(string messageTemplate)
        => Assert.Single(_events, e => e.MessageTemplate.Text == messageTemplate);
}
