using FluentValidation;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

namespace FluxusManager.Application.Validators.SolicitacoesCadastroValidators;

public class TokenSolicitacaoCadastroRequestValidator : AbstractValidator<TokenSolicitacaoCadastroRequest>
{
    public TokenSolicitacaoCadastroRequestValidator()
    {
        // Gerado por SecretToken.Create: 64 caracteres hexadecimais maiúsculos.
        RuleFor(x => x.Token).NotEmpty().Matches("^[0-9A-F]{64}$")
            .WithMessage("Link inválido. Abra novamente o link recebido por e-mail.");
    }
}
