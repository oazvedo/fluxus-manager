using FluentValidation;
using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Validators.EmpresasValidators;

public class CriarEmpresaRequestValidator : AbstractValidator<CriarEmpresaRequest>
{
    public CriarEmpresaRequestValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(150);

        RuleFor(x => x.NomeFantasia).MaximumLength(150);

        RuleFor(x => x.Cnpj)
            .NotEmpty()
            .Must(cnpj => Cnpj.EhValido(Cnpj.Normalizar(cnpj)))
            .WithMessage("Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.");
    }
}
