using FluentValidation;

namespace FluxusManager.Application.Validators.UsuariosValidators;

internal static class SenhaRule
{
    /// <summary>Política de senha do usuário: cadastro e aceite de convite usam a mesma regra.</summary>
    public static IRuleBuilderOptions<T, string> Senha<T>(this IRuleBuilder<T, string> rule)
        => rule
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("A senha deve conter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.");
}
