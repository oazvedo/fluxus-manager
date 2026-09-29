using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluxusManager.Infrastructure.Email;

/// <summary>
/// Envia os e-mails pendentes da solicitação de cadastro assim que uma rota pública sinaliza, e reenvia as falhas
/// (com espera crescente) a cada meio minuto. Uma falha aqui é registrada e nunca derruba a API.
/// </summary>
public class SolicitacaoCadastroEmailWorker(
    ISolicitacaoCadastroEmails emails,
    IOptions<SolicitacaoCadastroOptions> options,
    ILogger<SolicitacaoCadastroEmailWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnvioAutomatico)
            return;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Lote cheio: pode haver mais, então não espera.
                    while (await emails.ProcessarDevidosAsync(stoppingToken) >= 50)
                    {
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Falha ao processar os e-mails das solicitações de cadastro.");
                }

                await emails.AguardarSinalAsync(Intervalo, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
