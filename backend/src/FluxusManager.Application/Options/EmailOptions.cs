namespace FluxusManager.Application.Options;

/// <summary>Servidor SMTP e remetente dos e-mails do sistema. Em Development aponta para o Mailpit do docker compose.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>Opcional: sem usuário, o envio é feito sem autenticação (caso do Mailpit).</summary>
    public string? Usuario { get; set; }

    public string? Senha { get; set; }

    /// <summary>Padrão <see cref="EmailSeguranca.StartTls"/>, que exige TLS; <c>Nenhuma</c> só em ambiente local.</summary>
    public EmailSeguranca Seguranca { get; set; } = EmailSeguranca.StartTls;

    public string RemetenteEmail { get; set; } = string.Empty;
    public string RemetenteNome { get; set; } = "FluxusManager";
    public int TimeoutSegundos { get; set; } = 30;
}

public enum EmailSeguranca
{
    /// <summary>Conexão sem criptografia.</summary>
    Nenhuma,

    /// <summary>Conecta sem TLS e exige o STARTTLS (normalmente porta 587).</summary>
    StartTls,

    /// <summary>TLS desde a conexão (normalmente porta 465).</summary>
    SslTls
}
