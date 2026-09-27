using System.Net;
using System.Net.Sockets;
using System.Text;
using FluxusManager.Application.Email;
using FluxusManager.Application.Options;
using FluxusManager.Infrastructure;
using FluxusManager.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FluxusManager.UnitTests.Email;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task Enviar_EntregaHtmlETextoComORemetenteConfigurado()
    {
        using var servidor = new SmtpFalso();
        var sender = CriarSender(servidor.Porta);
        var mensagem = EmailTemplates.Convite("ana@empresa.com", "Acme Ltda", "https://app.fluxus.local/convite?t=1", TimeSpan.FromHours(24));

        await sender.EnviarAsync(mensagem);

        var recebido = await servidor.MensagemAsync();
        Assert.Equal("nao-responda@fluxus.local", recebido.From.Mailboxes.Single().Address);
        Assert.Equal("FluxusManager", recebido.From.Mailboxes.Single().Name);
        Assert.Equal("ana@empresa.com", recebido.To.Mailboxes.Single().Address);
        Assert.Equal(mensagem.Assunto, recebido.Subject);
        Assert.Equal(mensagem.Html, recebido.HtmlBody);
        Assert.Equal(mensagem.Texto, recebido.TextBody);
        Assert.Null(servidor.Credenciais);
    }

    [Fact]
    public async Task Enviar_ComUsuario_Autentica()
    {
        using var servidor = new SmtpFalso();
        var sender = CriarSender(servidor.Porta, usuario: "smtp-user", senha: "smtp-pass");

        await sender.EnviarAsync(new MensagemEmail("ana@empresa.com", "Teste", "<p>Oi</p>", "Oi"));

        await servidor.MensagemAsync();
        Assert.Equal("\0smtp-user\0smtp-pass", servidor.Credenciais);
    }

    [Theory]
    [InlineData("Email:Host", "")]
    [InlineData("Email:RemetenteEmail", "sem-arroba")]
    [InlineData("Email:RemetenteEmail", "Nome <x@fluxus.local>")]
    [InlineData("Email:Port", "0")]
    [InlineData("Email:TimeoutSegundos", "0")]
    public void Configuracao_Invalida_FalhaNaValidacao(string chave, string valor)
    {
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fluxus_tests",
            ["Email:Host"] = "localhost",
            ["Email:RemetenteEmail"] = "nao-responda@fluxus.local",
            [chave] = valor
        };
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddInfraModule(new ConfigurationBuilder().AddInMemoryCollection(configuracao).Build())
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
    }

    private static SmtpEmailSender CriarSender(int porta, string? usuario = null, string? senha = null)
        => new(Options.Create(new EmailOptions
        {
            Host = "127.0.0.1",
            Port = porta,
            Seguranca = EmailSeguranca.Nenhuma,
            Usuario = usuario,
            Senha = senha,
            RemetenteEmail = "nao-responda@fluxus.local",
            TimeoutSegundos = 5
        }), NullLogger<SmtpEmailSender>.Instance);

    /// <summary>Servidor SMTP mínimo em memória: aceita uma conexão, AUTH PLAIN e uma mensagem.</summary>
    private sealed class SmtpFalso : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Task<MimeMessage> _recebimento;

        public SmtpFalso()
        {
            _listener.Start();
            _recebimento = Task.Run(AtenderAsync);
        }

        public int Porta => ((IPEndPoint)_listener.LocalEndpoint).Port;

        /// <summary>Conteúdo decodificado do AUTH PLAIN (<c>\0usuario\0senha</c>), se houve autenticação.</summary>
        public string? Credenciais { get; private set; }

        public Task<MimeMessage> MensagemAsync() => _recebimento.WaitAsync(TimeSpan.FromSeconds(10));

        private async Task<MimeMessage> AtenderAsync()
        {
            using var cliente = await _listener.AcceptTcpClientAsync();
            var stream = cliente.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            MimeMessage? mensagem = null;

            await writer.WriteLineAsync("220 localhost ESMTP");
            while (await reader.ReadLineAsync() is { } linha)
            {
                var comando = linha.ToUpperInvariant();
                if (comando.StartsWith("EHLO"))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 AUTH PLAIN");
                }
                else if (comando.StartsWith("AUTH PLAIN"))
                {
                    var inicial = linha.Length > "AUTH PLAIN ".Length ? linha["AUTH PLAIN ".Length..] : null;
                    if (inicial is null)
                    {
                        await writer.WriteLineAsync("334 ");
                        inicial = await reader.ReadLineAsync();
                    }
                    Credenciais = Encoding.UTF8.GetString(Convert.FromBase64String(inicial!));
                    await writer.WriteLineAsync("235 2.7.0 Authentication successful");
                }
                else if (comando == "DATA")
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    var dados = new StringBuilder();
                    while (await reader.ReadLineAsync() is { } conteudo && conteudo != ".")
                        dados.Append(conteudo.StartsWith("..") ? conteudo[1..] : conteudo).Append("\r\n");
                    mensagem = MimeMessage.Load(new MemoryStream(Encoding.ASCII.GetBytes(dados.ToString())));
                    await writer.WriteLineAsync("250 OK");
                }
                else if (comando == "QUIT")
                {
                    await writer.WriteLineAsync("221 Bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }

            return mensagem ?? throw new InvalidOperationException("Nenhuma mensagem recebida.");
        }

        public void Dispose() => _listener.Stop();
    }
}
