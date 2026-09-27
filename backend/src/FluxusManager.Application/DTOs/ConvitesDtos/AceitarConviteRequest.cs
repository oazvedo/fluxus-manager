namespace FluxusManager.Application.DTOs.ConvitesDtos;

/// <summary>Nome e senha são obrigatórios só quando ainda não existe usuário com o e-mail do convite.</summary>
public record AceitarConviteRequest(string Token, string? Nome, string? Senha);
