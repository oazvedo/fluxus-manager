using System.ComponentModel.DataAnnotations;
using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

[ApiController]
[Route("empresas")]
public class EmpresasController(IEmpresaService empresaService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<EmpresaResponse>> Listar(
        [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")] int page = 1,
        [Range(1, 100, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => empresaService.ListarAsync(page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<EmpresaResponse> Obter(Guid id, CancellationToken cancellationToken)
        => empresaService.ObterAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<EmpresaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarEmpresaRequest request, CancellationToken cancellationToken)
    {
        var empresa = await empresaService.CriarAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = empresa.Id }, empresa);
    }

    [HttpPut("{id:guid}")]
    public Task<EmpresaResponse> Atualizar(Guid id, AtualizarEmpresaRequest request, CancellationToken cancellationToken)
        => empresaService.AtualizarAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        await empresaService.AtivarAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        await empresaService.InativarAsync(id, cancellationToken);

        return NoContent();
    }
}
