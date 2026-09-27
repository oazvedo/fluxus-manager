using FluxusManager.Application.Email;

namespace FluxusManager.UnitTests.Email;

public class EmailTemplatesTests
{
    private const string Link = "https://app.fluxus.local/convites/aceitar?token=abc&x=1";

    [Fact]
    public void Convite_MontaAssuntoDestinatarioELink()
    {
        var mensagem = EmailTemplates.Convite("ana@empresa.com", "Acme Ltda", Link, TimeSpan.FromHours(48));

        Assert.Equal("ana@empresa.com", mensagem.Para);
        Assert.Equal("Convite para acessar Acme Ltda no FluxusManager", mensagem.Assunto);
        Assert.Contains("href=\"https://app.fluxus.local/convites/aceitar?token=abc&amp;x=1\"", mensagem.Html);
        Assert.Contains(Link, mensagem.Texto);
        Assert.Contains("48 horas", mensagem.Texto);
    }

    [Fact]
    public void RecuperacaoSenha_MontaTextoComNomeEValidade()
    {
        var mensagem = EmailTemplates.RecuperacaoSenha("ana@empresa.com", "Ana", Link, TimeSpan.FromMinutes(30));

        Assert.Equal("Redefinição de senha do FluxusManager", mensagem.Assunto);
        Assert.Contains("Olá, Ana.", mensagem.Texto);
        Assert.Contains("30 minutos", mensagem.Html);
        Assert.Contains("ignore este e-mail", mensagem.Texto);
    }

    [Fact]
    public void Convite_EscapaValoresNoHtml()
    {
        var mensagem = EmailTemplates.Convite("ana@empresa.com", "<script>alert(1)</script>", Link, TimeSpan.FromHours(1));

        Assert.DoesNotContain("<script>", mensagem.Html);
        Assert.Contains("&lt;script&gt;", mensagem.Html);
        Assert.Contains("1 hora.", mensagem.Texto);
    }

    [Theory]
    [InlineData("/convites/aceitar")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://app.fluxus.local/x")]
    public void Convite_ComLinkQueNaoEHttpAbsoluto_Falha(string link)
    {
        Assert.Throws<ArgumentException>(() => EmailTemplates.Convite("ana@empresa.com", "Acme", link, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RecuperacaoSenha_ComValidadeNaoPositiva_Falha()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmailTemplates.RecuperacaoSenha("ana@empresa.com", "Ana", Link, TimeSpan.Zero));
    }
}
