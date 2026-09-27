using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FluxusManager.Infrastructure.Email;

/// <summary>Envia pelo servidor SMTP de <see cref="EmailOptions"/>, com uma conexão por mensagem.</summary>
public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var mime = Montar(mensagem, settings);

        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(settings.TimeoutSegundos).TotalMilliseconds };
        await client.ConnectAsync(settings.Host, settings.Port, Seguranca(settings.Seguranca), cancellationToken);
        if (!string.IsNullOrEmpty(settings.Usuario))
            await client.AuthenticateAsync(settings.Usuario, settings.Senha ?? string.Empty, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        // Sem destinatário no log: e-mail é dado pessoal.
        logger.LogInformation("E-mail {MessageId} enviado: {Assunto}", mime.MessageId, mensagem.Assunto);
    }

    private static MimeMessage Montar(MensagemEmail mensagem, EmailOptions settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.RemetenteNome, settings.RemetenteEmail));
        mime.To.Add(MailboxAddress.Parse(mensagem.Para));
        mime.Subject = mensagem.Assunto;
        mime.Body = new BodyBuilder { HtmlBody = mensagem.Html, TextBody = mensagem.Texto }.ToMessageBody();
        return mime;
    }

    private static SecureSocketOptions Seguranca(EmailSeguranca seguranca) => seguranca switch
    {
        EmailSeguranca.Nenhuma => SecureSocketOptions.None,
        EmailSeguranca.StartTls => SecureSocketOptions.StartTls,
        EmailSeguranca.SslTls => SecureSocketOptions.SslOnConnect,
        _ => throw new ArgumentOutOfRangeException(nameof(seguranca), seguranca, null)
    };
}
