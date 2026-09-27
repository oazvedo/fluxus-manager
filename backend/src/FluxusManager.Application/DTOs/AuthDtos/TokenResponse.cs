namespace FluxusManager.Application.DTOs.AuthDtos;

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, Guid TenantId, string Role, IReadOnlyCollection<string> Permissions, string? RefreshToken = null, DateTime? RefreshTokenExpiresAt = null);
