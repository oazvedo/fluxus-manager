using FluentValidation;

namespace FluxusManager.Application.Validators.ConvitesValidators;

internal static class TokenDeConviteRule
{
    /// <summary>O token é gerado por <c>SecretToken.Create</c>: 64 caracteres hexadecimais maiúsculos.</summary>
    public static IRuleBuilderOptions<T, string> TokenDeConvite<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().Matches("^[0-9A-F]{64}$").WithMessage("Link de convite inválido. Abra novamente o link recebido por e-mail.");
}
