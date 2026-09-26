using FluxusManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.IntegrationTests.Errors;

/// <summary>Controller usado só nos testes para disparar cada tipo de erro.</summary>
[ApiController]
[Route("test/erros")]
public class ErrosTestController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult NaoEncontrado() => throw new NotFoundException("Empresa", 42);

    [HttpGet("conflict")]
    public IActionResult Conflito() => throw new ConflictException("CNPJ já cadastrado.");

    [HttpGet("business-rule")]
    public IActionResult RegraDeNegocio() => throw new BusinessRuleException("Empresa inativa não pode ter filiais.");

    [HttpGet("unexpected")]
    public IActionResult Inesperado() => throw new InvalidOperationException("detalhe interno sensível");
}
