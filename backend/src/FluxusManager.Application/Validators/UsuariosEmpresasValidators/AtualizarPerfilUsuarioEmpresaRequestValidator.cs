using FluentValidation;
using FluxusManager.Application.DTOs.UsuariosEmpresasDtos;

namespace FluxusManager.Application.Validators.UsuariosEmpresasValidators;

public class AtualizarPerfilUsuarioEmpresaRequestValidator : AbstractValidator<AtualizarPerfilUsuarioEmpresaRequest>
{
    public AtualizarPerfilUsuarioEmpresaRequestValidator() => RuleFor(x => x.Perfil).NotEmpty().MaximumLength(50);
}
