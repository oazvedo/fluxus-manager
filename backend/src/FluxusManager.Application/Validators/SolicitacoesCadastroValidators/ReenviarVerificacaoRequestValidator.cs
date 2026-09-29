using FluentValidation;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

namespace FluxusManager.Application.Validators.SolicitacoesCadastroValidators;

public class ReenviarVerificacaoRequestValidator : AbstractValidator<ReenviarVerificacaoRequest>
{
    public ReenviarVerificacaoRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}
