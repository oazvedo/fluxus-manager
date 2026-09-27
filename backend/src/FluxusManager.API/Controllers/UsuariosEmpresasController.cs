using FluxusManager.Application.DTOs.UsuariosEmpresasDtos;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FluxusManager.API.Security;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("usuarios-empresas")]
public class UsuariosEmpresasController(IUsuarioEmpresaService vinculoService) : ControllerBase
{
    [HttpGet("por-usuario/{usuarioId:guid}")]
    [HasPermission("usuarios-empresas.visualizar")]
    public Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorUsuario(Guid usuarioId, CancellationToken cancellationToken)
        => vinculoService.ListarPorUsuarioAsync(usuarioId, cancellationToken);

    [HttpGet("por-empresa/{empresaId:guid}")]
    [HasPermission("usuarios-empresas.visualizar")]
    public Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorEmpresa(Guid empresaId, CancellationToken cancellationToken)
        => vinculoService.ListarPorEmpresaAsync(empresaId, cancellationToken);

    [HttpPost]
    [HasPermission("usuarios-empresas.editar")]
    [ProducesResponseType<UsuarioEmpresaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Vincular(VincularUsuarioEmpresaRequest request, CancellationToken cancellationToken)
    {
        var vinculo = await vinculoService.VincularAsync(request, cancellationToken);
        return Created($"/usuarios-empresas/por-usuario/{vinculo.UsuarioId}", vinculo);
    }

    [HttpPut("{usuarioId:guid}/{empresaId:guid}")]
    [HasPermission("usuarios-empresas.editar")]
    public Task<UsuarioEmpresaResponse> AtualizarPerfil(Guid usuarioId, Guid empresaId,
        AtualizarPerfilUsuarioEmpresaRequest request, CancellationToken cancellationToken)
        => vinculoService.AtualizarPerfilAsync(usuarioId, empresaId, request, cancellationToken);

    [HttpDelete("{usuarioId:guid}/{empresaId:guid}")]
    [HasPermission("usuarios-empresas.editar")]
    public async Task<IActionResult> Desvincular(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken)
    { await vinculoService.DesvincularAsync(usuarioId, empresaId, cancellationToken); return NoContent(); }

    [HttpPatch("{usuarioId:guid}/{empresaId:guid}/ativar")]
    [HasPermission("usuarios-empresas.editar")]
    public async Task<IActionResult> Ativar(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken)
    { await vinculoService.AtivarAsync(usuarioId, empresaId, cancellationToken); return NoContent(); }

    [HttpPatch("{usuarioId:guid}/{empresaId:guid}/inativar")]
    [HasPermission("usuarios-empresas.editar")]
    public async Task<IActionResult> Inativar(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken)
    { await vinculoService.InativarAsync(usuarioId, empresaId, cancellationToken); return NoContent(); }
}
