using FluentValidation;
using FluxusManager.Application.DTOs.UsuariosEmpresasDtos;

namespace FluxusManager.Application.Validators.UsuariosEmpresasValidators;

public class VincularUsuarioEmpresaRequestValidator : AbstractValidator<VincularUsuarioEmpresaRequest>
{
    public VincularUsuarioEmpresaRequestValidator()
    {
        RuleFor(x => x.UsuarioId).NotEmpty();
        RuleFor(x => x.EmpresaId).NotEmpty();
        RuleFor(x => x.Perfil).NotEmpty().MaximumLength(50);
    }
}
