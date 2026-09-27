using System.ComponentModel.DataAnnotations;
using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using FluxusManager.API.Security;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("usuarios")]
public class UsuariosController(IUsuarioService usuarioService) : ControllerBase
{
    [HttpGet]
    [HasPermission("usuarios.visualizar")]
    public Task<PagedResult<UsuarioResponse>> Listar(
        [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")] int page = 1,
        [Range(1, 100, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => usuarioService.ListarAsync(page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission("usuarios.visualizar")]
    public Task<UsuarioResponse> Obter(Guid id, CancellationToken cancellationToken)
        => usuarioService.ObterAsync(id, cancellationToken);

    [HttpPost]
    [HasPermission("usuarios.editar")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await usuarioService.CriarAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("usuarios.editar")]
    public Task<UsuarioResponse> Atualizar(Guid id, AtualizarUsuarioRequest request, CancellationToken cancellationToken)
        => usuarioService.AtualizarAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/ativar")]
    [HasPermission("usuarios.editar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        await usuarioService.AtivarAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    [HasPermission("usuarios.editar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        await usuarioService.InativarAsync(id, cancellationToken);

        return NoContent();
    }
}
