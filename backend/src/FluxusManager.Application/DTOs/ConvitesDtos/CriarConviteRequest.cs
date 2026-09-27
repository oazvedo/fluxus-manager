namespace FluxusManager.Application.DTOs.ConvitesDtos;

/// <summary>A empresa é a selecionada no token; o perfil precisa estar ativo nela.</summary>
public record CriarConviteRequest(string Email, Guid PerfilId);
