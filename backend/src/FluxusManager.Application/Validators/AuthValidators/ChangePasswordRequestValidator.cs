using FluentValidation;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Validators.UsuariosValidators;

namespace FluxusManager.Application.Validators.AuthValidators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty();
        RuleFor(x => x.NovaSenha).NotEmpty().Senha();
    }
}
