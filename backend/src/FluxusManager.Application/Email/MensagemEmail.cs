namespace FluxusManager.Application.Email;

/// <summary>E-mail pronto para envio, com as versões HTML e texto do mesmo conteúdo.</summary>
public record MensagemEmail(string Para, string Assunto, string Html, string Texto);
