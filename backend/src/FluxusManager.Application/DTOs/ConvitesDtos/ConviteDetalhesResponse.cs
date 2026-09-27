namespace FluxusManager.Application.DTOs.ConvitesDtos;

/// <summary>Dados da tela de aceite. <c>UsuarioExistente</c> indica se o aceite dispensa nome e senha.</summary>
public record ConviteDetalhesResponse(string Email, string Empresa, string Perfil, DateTime ExpiraEm, bool UsuarioExistente);
