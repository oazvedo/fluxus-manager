using FluentValidation;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Validators.SolicitacoesCadastroValidators;

public class CriarSolicitacaoCadastroRequestValidator : AbstractValidator<CriarSolicitacaoCadastroRequest>
{
    public CriarSolicitacaoCadastroRequestValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NomeFantasia).MaximumLength(150);
        RuleFor(x => x.Cnpj)
            .NotEmpty()
            .Must(cnpj => Cnpj.EhValido(Cnpj.Normalizar(cnpj)))
            .WithMessage("Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.");
        RuleFor(x => x.ResponsavelNome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ResponsavelEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.ResponsavelTelefone)
            .MaximumLength(20)
            .Matches(@"^[0-9+()\-\s]{8,20}$").WithMessage("Informe um telefone com DDD, só com números, espaços, +, ( ) e -.")
            .When(x => !string.IsNullOrWhiteSpace(x.ResponsavelTelefone));
    }
}
