using FluentValidation;
using FluxusManager.Application.DTOs.UsuariosDtos;

namespace FluxusManager.Application.Validators.UsuariosValidators;

public class AtualizarUsuarioRequestValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    public AtualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}
