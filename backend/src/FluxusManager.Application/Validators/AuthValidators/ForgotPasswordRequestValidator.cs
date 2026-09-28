using FluentValidation;
using FluxusManager.Application.DTOs.AuthDtos;

namespace FluxusManager.Application.Validators.AuthValidators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
        => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
}
