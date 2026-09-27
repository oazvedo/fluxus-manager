using FluentValidation;
using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.Validators.UsuariosValidators;

namespace FluxusManager.Application.Validators.ConvitesValidators;

public class AceitarConviteRequestValidator : AbstractValidator<AceitarConviteRequest>
{
    public AceitarConviteRequestValidator()
    {
        RuleFor(x => x.Token).TokenDeConvite();
        RuleFor(x => x.Nome).MaximumLength(150);
        // A obrigatoriedade depende de o e-mail já ter usuário (service); aqui, só o formato.
        RuleFor(x => x.Senha!).Senha().When(x => !string.IsNullOrEmpty(x.Senha));
    }
}
