namespace FluxusManager.Application.Options;

/// <summary>Prazo dos links de redefinição de senha.</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "RecuperacaoSenha";
    public int ValidadeMinutos { get; set; } = 60;
}
