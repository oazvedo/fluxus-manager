using FluentValidation;
using FluxusManager.Application.DTOs.FiliaisDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Validators.FiliaisValidators;

public class CriarFilialRequestValidator : AbstractValidator<CriarFilialRequest>
{
    public CriarFilialRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Cnpj).NotEmpty().Must(cnpj => Cnpj.EhValido(Cnpj.Normalizar(cnpj)))
            .WithMessage("Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.");
    }
}
