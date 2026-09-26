using FluentValidation;
using FluxusManager.Application.DTOs.UsuariosDtos;

namespace FluxusManager.Application.Validators.UsuariosValidators;

public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);

        RuleFor(x => x.Senha)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("A senha deve conter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.");
    }
}
