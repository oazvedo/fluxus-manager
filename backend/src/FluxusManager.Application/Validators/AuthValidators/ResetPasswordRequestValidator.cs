using FluentValidation;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Validators.UsuariosValidators;

namespace FluxusManager.Application.Validators.AuthValidators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().Length(64).Matches("^[0-9A-F]{64}$");
        RuleFor(x => x.NovaSenha).NotEmpty().Senha();
    }
}
