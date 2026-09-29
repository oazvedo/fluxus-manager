namespace FluxusManager.Application.Interfaces;

/// <summary>
/// Entrega dos e-mails da solicitação de cadastro (outbox). Cada envio roda no próprio escopo, gera um token novo
/// para o link e só então envia; uma falha fica registrada e é tentada de novo com espera crescente.
/// </summary>
public interface ISolicitacaoCadastroEmails
{
    /// <summary>Avisa a rotina em segundo plano de que há e-mail novo, sem esperar o envio (rotas públicas).</summary>
    void Sinalizar();

    /// <summary>Espera um <see cref="Sinalizar"/> ou o fim do prazo, o que vier antes.</summary>
    Task AguardarSinalAsync(TimeSpan prazo, CancellationToken cancellationToken = default);

    /// <summary>Envia os e-mails cuja próxima tentativa já venceu. Devolve quantos foram processados.</summary>
    Task<int> ProcessarDevidosAsync(CancellationToken cancellationToken = default);

    /// <summary>Envia agora os e-mails não entregues de uma solicitação (ações do administrador).</summary>
    Task ProcessarSolicitacaoAsync(Guid solicitacaoId, CancellationToken cancellationToken = default);
}
