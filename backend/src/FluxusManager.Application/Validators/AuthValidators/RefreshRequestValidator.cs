using FluentValidation;
using FluxusManager.Application.DTOs.AuthDtos;

namespace FluxusManager.Application.Validators.AuthValidators;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().Length(64).Matches("^[0-9A-F]{64}$");
        RuleFor(x => x.EmpresaId).NotEqual(Guid.Empty).When(x => x.EmpresaId.HasValue);
    }
}
