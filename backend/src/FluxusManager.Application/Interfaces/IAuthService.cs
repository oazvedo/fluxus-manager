using FluxusManager.Application.DTOs.AuthDtos;

namespace FluxusManager.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<TokenResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task EsquecerSenhaAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task RedefinirSenhaAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task TrocarSenhaAsync(Guid usuarioId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<TokenResponse> SwitchTenantAsync(Guid userId, Guid empresaId, CancellationToken cancellationToken = default);
}
