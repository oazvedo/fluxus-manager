using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxusManager.Infrastructure.Database;

/// <summary>
/// Rotina diária das partições de audit.change_log: cria as dos próximos meses e, com
/// <c>Auditoria:RetencaoMeses</c> maior que zero, apaga as mais antigas que isso.
/// Desligada com <c>Auditoria:ManutencaoParticoes=false</c>.
/// </summary>
public class AuditPartitionMaintenance(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuditPartitionMaintenance> logger) : BackgroundService
{
    private const int MonthsAhead = 3;
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Auditoria:ManutencaoParticoes", true))
            return;

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;

            await database.ExecuteSqlAsync($"SELECT audit.ensure_partitions({MonthsAhead})", cancellationToken);

            var retentionMonths = configuration.GetValue("Auditoria:RetencaoMeses", 0);
            if (retentionMonths > 0)
            {
                var dropped = await database
                    .SqlQuery<int>($"SELECT audit.drop_partitions_older_than(make_interval(months => {retentionMonths})) AS \"Value\"")
                    .SingleAsync(cancellationToken);

                if (dropped > 0)
                    logger.LogInformation("Auditoria: {Quantidade} partição(ões) com mais de {Meses} meses apagada(s).", dropped, retentionMonths);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falha aqui não pode derrubar a API: a partição default recebe as linhas até a próxima execução.
            logger.LogError(ex, "Auditoria: falha na manutenção das partições de audit.change_log.");
        }
    }
}
