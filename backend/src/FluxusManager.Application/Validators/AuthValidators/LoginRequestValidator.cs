using FluxusManager.Application.DTOs.AuthDtos;
using FluentValidation;

namespace FluxusManager.Application.Validators.AuthValidators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Senha).NotEmpty();
    }
}
