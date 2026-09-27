using FluentValidation;
using FluxusManager.Application.DTOs.FiliaisDtos;

namespace FluxusManager.Application.Validators.FiliaisValidators;

public class AtualizarFilialRequestValidator : AbstractValidator<AtualizarFilialRequest>
{
    public AtualizarFilialRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(300);
    }
}
