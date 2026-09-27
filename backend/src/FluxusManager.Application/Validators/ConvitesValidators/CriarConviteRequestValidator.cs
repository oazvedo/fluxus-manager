using FluentValidation;
using FluxusManager.Application.DTOs.ConvitesDtos;

namespace FluxusManager.Application.Validators.ConvitesValidators;

public class CriarConviteRequestValidator : AbstractValidator<CriarConviteRequest>
{
    public CriarConviteRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PerfilId).NotEmpty();
    }
}
