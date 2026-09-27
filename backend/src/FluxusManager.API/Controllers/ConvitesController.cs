using System.ComponentModel.DataAnnotations;
using FluxusManager.API.Security;
using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("convites")]
public class ConvitesController(IConviteService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("usuarios-empresas.visualizar")]
    public Task<PagedResult<ConviteResponse>> Listar(
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => service.ListarAsync(page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission("usuarios-empresas.visualizar")]
    public Task<ConviteResponse> Obter(Guid id, CancellationToken cancellationToken)
        => service.ObterAsync(id, cancellationToken);

    [HttpPost]
    [HasPermission("usuarios-empresas.editar")]
    [ProducesResponseType<ConviteResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarConviteRequest request, CancellationToken cancellationToken)
    {
        var convite = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = convite.Id }, convite);
    }

    [HttpPost("{id:guid}/reenviar")]
    [HasPermission("usuarios-empresas.editar")]
    public Task<ConviteResponse> Reenviar(Guid id, CancellationToken cancellationToken)
        => service.ReenviarAsync(id, cancellationToken);

    [HttpPatch("{id:guid}/cancelar")]
    [HasPermission("usuarios-empresas.editar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    { await service.CancelarAsync(id, cancellationToken); return NoContent(); }

    [AllowAnonymous]
    [HttpPost("consultar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ConviteDetalhesResponse> Consultar(ConsultarConviteRequest request, CancellationToken cancellationToken)
        => service.ConsultarAsync(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("aceitar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Aceitar(AceitarConviteRequest request, CancellationToken cancellationToken)
    { await service.AceitarAsync(request, cancellationToken); return NoContent(); }
}
