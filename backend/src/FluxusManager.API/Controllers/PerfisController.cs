using System.ComponentModel.DataAnnotations;
using FluxusManager.API.Security;
using FluxusManager.Application.DTOs.PerfisDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("perfis")]
public class PerfisController(IPerfilService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("perfis.visualizar")]
    public Task<PagedResult<PerfilResponse>> Listar(
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => service.ListarAsync(page, pageSize, cancellationToken);

    [HttpGet("permissoes")]
    [HasPermission("perfis.visualizar")]
    public IReadOnlyCollection<PermissaoResponse> ListarPermissoes() => service.ListarPermissoes();

    [HttpGet("{id:guid}")]
    [HasPermission("perfis.visualizar")]
    public Task<PerfilResponse> Obter(Guid id, CancellationToken cancellationToken)
        => service.ObterAsync(id, cancellationToken);

    [HttpPost]
    [HasPermission("perfis.editar")]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarPerfilRequest request, CancellationToken cancellationToken)
    {
        var perfil = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = perfil.Id }, perfil);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("perfis.editar")]
    public Task<PerfilResponse> Atualizar(Guid id, AtualizarPerfilRequest request, CancellationToken cancellationToken)
        => service.AtualizarAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/ativar")]
    [HasPermission("perfis.editar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    { await service.AtivarAsync(id, cancellationToken); return NoContent(); }

    [HttpPatch("{id:guid}/inativar")]
    [HasPermission("perfis.editar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    { await service.InativarAsync(id, cancellationToken); return NoContent(); }

    [HttpDelete("{id:guid}")]
    [HasPermission("perfis.editar")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    { await service.ExcluirAsync(id, cancellationToken); return NoContent(); }
}
