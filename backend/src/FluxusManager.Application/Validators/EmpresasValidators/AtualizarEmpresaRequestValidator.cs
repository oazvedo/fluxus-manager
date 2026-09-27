using FluentValidation;
using FluxusManager.Application.DTOs.EmpresasDtos;

namespace FluxusManager.Application.Validators.EmpresasValidators;

public class AtualizarEmpresaRequestValidator : AbstractValidator<AtualizarEmpresaRequest>
{
    public AtualizarEmpresaRequestValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(150);

        RuleFor(x => x.NomeFantasia).MaximumLength(150);
    }
}
