using System.Net;

namespace FluxusManager.Application.Email;

/// <summary>
/// Modelos dos e-mails do sistema. Cada um gera as versões HTML e texto; os valores recebidos são escapados no HTML.
/// Os links chegam prontos (com o token) de quem pede o envio e precisam ser absolutos e HTTP(S).
/// </summary>
public static class EmailTemplates
{
    private const string Produto = "FluxusManager";

    public static MensagemEmail Convite(string para, string nomeEmpresa, string link, TimeSpan validade)
    {
        var assunto = $"Convite para acessar {nomeEmpresa} no {Produto}";
        var paragrafos = new[]
        {
            $"Você recebeu um convite para acessar a empresa {nomeEmpresa} no {Produto}.",
            $"Para aceitar, abra o link abaixo e conclua o cadastro. O link vale por {Validade(validade)}."
        };
        const string rodape = "Se você não esperava este convite, ignore este e-mail.";

        return Montar(para, assunto, "Convite de acesso", paragrafos, "Aceitar convite", link, rodape);
    }

    public static MensagemEmail RecuperacaoSenha(string para, string nome, string link, TimeSpan validade)
    {
        var assunto = $"Redefinição de senha do {Produto}";
        var paragrafos = new[]
        {
            $"Olá, {nome}.",
            $"Recebemos um pedido para redefinir a sua senha. Abra o link abaixo para criar uma nova. O link vale por {Validade(validade)} e só pode ser usado uma vez."
        };
        const string rodape = "Se você não pediu a redefinição, ignore este e-mail: a sua senha continua a mesma.";

        return Montar(para, assunto, "Redefinir senha", paragrafos, "Redefinir senha", link, rodape);
    }

    public static MensagemEmail VerificacaoSolicitacao(string para, string nome, string razaoSocial, string link, TimeSpan validade)
    {
        var assunto = $"Confirme o seu e-mail para o cadastro de {razaoSocial} no {Produto}";
        var paragrafos = new[]
        {
            $"Olá, {nome}.",
            $"Recebemos o pedido de cadastro da empresa {razaoSocial}. Confirme que este e-mail é seu para enviarmos o pedido à análise. O link vale por {Validade(validade)} e só pode ser usado uma vez."
        };
        const string rodape = "Se você não fez este pedido, ignore este e-mail: nada será cadastrado.";

        return Montar(para, assunto, "Confirme o seu e-mail", paragrafos, "Confirmar e-mail", link, rodape);
    }

    public static MensagemEmail SolicitacaoEmAnalise(string para, string nome, string razaoSocial, string link, int validadeDias)
    {
        var assunto = $"Pedido de cadastro de {razaoSocial} em análise";
        var paragrafos = new[]
        {
            $"Olá, {nome}.",
            $"O seu e-mail foi confirmado e o pedido de cadastro da empresa {razaoSocial} está em análise pela equipe do {Produto}. Avisaremos por e-mail quando houver uma decisão.",
            $"Use o link abaixo para acompanhar o pedido. Ele vale por {validadeDias} dias e só mostra a situação, sem permitir alterações."
        };
        const string rodape = "Guarde este e-mail. Se você não fez este pedido, ignore esta mensagem.";

        return Montar(para, assunto, "Pedido em análise", paragrafos, "Acompanhar pedido", link, rodape);
    }

    public static MensagemEmail SolicitacaoAprovada(string para, string nome, string razaoSocial, string link, TimeSpan validade)
    {
        var assunto = $"Cadastro de {razaoSocial} aprovado no {Produto}";
        var paragrafos = new[]
        {
            $"Olá, {nome}.",
            $"O cadastro da empresa {razaoSocial} foi aprovado e você foi convidado como administrador dela.",
            $"Abra o link abaixo para aceitar o convite e criar a sua senha, ou entrar com a conta que você já tem. O link vale por {Validade(validade)}."
        };
        const string rodape = "Se o link vencer, peça um novo envio à equipe do FluxusManager.";

        return Montar(para, assunto, "Cadastro aprovado", paragrafos, "Aceitar convite", link, rodape);
    }

    public static MensagemEmail SolicitacaoRecusada(string para, string nome, string razaoSocial, string motivo, string link)
    {
        var assunto = $"Pedido de cadastro de {razaoSocial} não aprovado";
        var paragrafos = new[]
        {
            $"Olá, {nome}.",
            $"Analisamos o pedido de cadastro da empresa {razaoSocial} e ele não foi aprovado.",
            $"Motivo: {motivo}",
            "Se a situação mudar, você pode enviar um novo pedido."
        };
        const string rodape = "O link acima só mostra a situação do pedido e não permite alterações.";

        return Montar(para, assunto, "Pedido não aprovado", paragrafos, "Ver o pedido", link, rodape);
    }

    private static MensagemEmail Montar(string para, string assunto, string titulo, string[] paragrafos, string acao, string link, string rodape)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("O link do e-mail deve ser uma URL absoluta HTTP(S).", nameof(link));

        var url = uri.AbsoluteUri;
        var texto = string.Join("\n\n", [titulo, .. paragrafos, $"{acao}: {url}", rodape, $"— {Produto}"]) + "\n";
        var html = Html(titulo, paragrafos, acao, url, rodape);

        return new MensagemEmail(para, assunto, html, texto);
    }

    private static string Html(string titulo, string[] paragrafos, string acao, string link, string rodape)
    {
        static string E(string valor) => WebUtility.HtmlEncode(valor);
        var corpo = string.Concat(paragrafos.Select(p =>
            $"""<p style="margin:0 0 16px;font-size:15px;line-height:1.5;color:#1f2328">{E(p)}</p>"""));

        return $"""
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><title>{E(titulo)}</title></head>
            <body style="margin:0;padding:24px;background:#f6f7f9;font-family:-apple-system,'Segoe UI',Roboto,Arial,sans-serif">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;margin:0 auto;background:#ffffff;border:1px solid #e4e6ea;border-radius:8px">
                <tr><td style="padding:32px">
                  <p style="margin:0 0 24px;font-size:13px;font-weight:600;color:#59636e">{Produto}</p>
                  <h1 style="margin:0 0 16px;font-size:20px;color:#1f2328">{E(titulo)}</h1>
                  {corpo}
                  <p style="margin:24px 0"><a href="{E(link)}" style="display:inline-block;padding:10px 18px;background:#1f2328;color:#ffffff;text-decoration:none;border-radius:6px;font-size:14px;font-weight:600">{E(acao)}</a></p>
                  <p style="margin:0 0 16px;font-size:13px;line-height:1.5;color:#59636e">Se o botão não funcionar, copie e cole este endereço no navegador:<br><a href="{E(link)}" style="color:#0969da;word-break:break-all">{E(link)}</a></p>
                  <p style="margin:24px 0 0;font-size:13px;line-height:1.5;color:#59636e">{E(rodape)}</p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Validade(TimeSpan validade)
    {
        if (validade <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(validade), "A validade do link deve ser positiva.");

        if (validade.TotalHours >= 1 && validade.TotalHours % 1 == 0)
            return validade.TotalHours == 1 ? "1 hora" : $"{validade.TotalHours:0} horas";

        var minutos = (int)Math.Ceiling(validade.TotalMinutes);
        return minutos == 1 ? "1 minuto" : $"{minutos} minutos";
    }
}
