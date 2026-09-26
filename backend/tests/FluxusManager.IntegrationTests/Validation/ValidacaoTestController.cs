using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.IntegrationTests.Validation;

public record EnderecoTestDto(string Cep);

public record CadastroTestDto(string Nome, string Email, EnderecoTestDto Endereco);

public class CadastroTestDtoValidator : AbstractValidator<CadastroTestDto>
{
    public CadastroTestDtoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Endereco.Cep).Length(8);
    }
}

/// <summary>Controller usado só nos testes de validação.</summary>
[ApiController]
[Route("test/validacao")]
public class ValidacaoTestController : ControllerBase
{
    public static int Chamadas;

    [HttpPost]
    public IActionResult Cadastrar(CadastroTestDto dto)
    {
        Interlocked.Increment(ref Chamadas);
        return Ok(dto);
    }
}
