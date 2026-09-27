namespace FluxusManager.Application.DTOs.ConvitesDtos;

/// <summary>Token recebido no link do e-mail. Vai no corpo, não na URL, para não aparecer em logs.</summary>
public record ConsultarConviteRequest(string Token);
