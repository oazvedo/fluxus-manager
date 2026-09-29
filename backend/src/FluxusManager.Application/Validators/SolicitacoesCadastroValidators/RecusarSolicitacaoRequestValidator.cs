using FluentValidation;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

namespace FluxusManager.Application.Validators.SolicitacoesCadastroValidators;

public class RecusarSolicitacaoRequestValidator : AbstractValidator<RecusarSolicitacaoRequest>
{
    public RecusarSolicitacaoRequestValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Informe o motivo da recusa: ele será enviado ao solicitante.")
            .Must(motivo => motivo?.Trim().Length is >= 10 and <= 500)
            .WithMessage("O motivo deve ter entre 10 e 500 caracteres.");
        RuleFor(x => x.ObservacaoInterna).MaximumLength(1000);
    }
}
