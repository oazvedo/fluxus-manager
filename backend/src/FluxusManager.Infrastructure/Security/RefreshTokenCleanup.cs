using FluxusManager.Application.Options;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluxusManager.Infrastructure.Security;

/// <summary>Remove diariamente famílias expiradas, preservando o histórico necessário à detecção de reuso.</summary>
public class RefreshTokenCleanup(
    IServiceScopeFactory scopeFactory,
    IOptions<RefreshTokenOptions> options,
    TimeProvider clock,
    ILogger<RefreshTokenCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.LimpezaHabilitada)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                    await repository.LimparExpiradosAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Falha na limpeza dos refresh tokens expirados.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
