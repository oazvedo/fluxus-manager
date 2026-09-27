using FluentValidation;
using FluxusManager.Application.DTOs.AuthDtos;

namespace FluxusManager.Application.Validators.AuthValidators;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().Length(64).Matches("^[0-9A-F]{64}$");
}
