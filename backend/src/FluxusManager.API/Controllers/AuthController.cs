using System.Security.Claims;
using FluxusManager.API.Security;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(request, cancellationToken);
        return token is null ? Unauthorized(new ProblemDetails { Title = "Credenciais inválidas ou usuário sem vínculo ativo com uma empresa." }) : Ok(token);
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
