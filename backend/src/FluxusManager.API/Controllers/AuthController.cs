using System.Security.Claims;
using FluxusManager.API.Security;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(request, cancellationToken);
        return token is null ? Unauthorized(new ProblemDetails { Title = "Credenciais inválidas ou usuário sem vínculo ativo com uma empresa." }) : Ok(token);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.EsquecerSenhaAsync(request, cancellationToken);
        return Ok(new { mensagem = "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.RedefinirSenhaAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
            return Unauthorized();

        await authService.TrocarSenhaAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [DisableRateLimiting]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new ProblemDetails { Title = "Sessão inválida ou expirada. Faça login novamente." })
            : Ok(response);
    }

    [AllowAnonymous]
    [DisableRateLimiting]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("switch-tenant")]
    public Task<IActionResult> SwitchTenant(SwitchTenantRequest request, CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
            return Task.FromResult<IActionResult>(Unauthorized());

        return CreateResponseAsync(userId, request.EmpresaId, cancellationToken);
    }

    private async Task<IActionResult> CreateResponseAsync(Guid userId, Guid empresaId, CancellationToken cancellationToken)
        => Ok(await authService.SwitchTenantAsync(userId, empresaId, cancellationToken));
}
