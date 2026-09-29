using FluentValidation;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

namespace FluxusManager.Application.Validators.SolicitacoesCadastroValidators;

public class AprovarSolicitacaoRequestValidator : AbstractValidator<AprovarSolicitacaoRequest>
{
    public AprovarSolicitacaoRequestValidator()
    {
        RuleFor(x => x.ObservacaoInterna).MaximumLength(1000);
    }
}
