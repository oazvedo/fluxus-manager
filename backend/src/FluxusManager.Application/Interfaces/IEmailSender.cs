using FluxusManager.Application.Email;

namespace FluxusManager.Application.Interfaces;

/// <summary>Envia e-mails do sistema. A implementação (SMTP) fica na Infrastructure; falha de envio lança exceção.</summary>
public interface IEmailSender
{
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default);
}
