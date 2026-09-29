namespace FluxusManager.Application.DTOs.AuthDtos;

/// <param name="AdministradorPlataforma">
/// Só orienta a interface (mostrar o painel global). A autorização de plataforma é conferida no banco a cada requisição.
/// </param>
public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, Guid TenantId, string Role, IReadOnlyCollection<string> Permissions, string? RefreshToken = null, DateTime? RefreshTokenExpiresAt = null, bool AdministradorPlataforma = false);
