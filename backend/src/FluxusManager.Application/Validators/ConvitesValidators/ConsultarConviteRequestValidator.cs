using FluentValidation;
using FluxusManager.Application.DTOs.ConvitesDtos;

namespace FluxusManager.Application.Validators.ConvitesValidators;

public class ConsultarConviteRequestValidator : AbstractValidator<ConsultarConviteRequest>
{
    public ConsultarConviteRequestValidator() => RuleFor(x => x.Token).TokenDeConvite();
}
