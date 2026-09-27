using System.ComponentModel.DataAnnotations;
using FluxusManager.Application.DTOs.FiliaisDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("filiais")]
public class FiliaisController(IFilialService filialService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<FilialResponse>> Listar([Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 20, CancellationToken cancellationToken = default)
        => filialService.ListarAsync(page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<FilialResponse> Obter(Guid id, CancellationToken cancellationToken) => filialService.ObterAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<FilialResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarFilialRequest request, CancellationToken cancellationToken)
    {
        var filial = await filialService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = filial.Id }, filial);
    }

    [HttpPut("{id:guid}")]
    public Task<FilialResponse> Atualizar(Guid id, AtualizarFilialRequest request, CancellationToken cancellationToken)
        => filialService.AtualizarAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    { await filialService.AtivarAsync(id, cancellationToken); return NoContent(); }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    { await filialService.InativarAsync(id, cancellationToken); return NoContent(); }
}
