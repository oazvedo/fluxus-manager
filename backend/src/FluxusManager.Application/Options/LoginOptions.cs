namespace FluxusManager.Application.Options;

/// <summary>Bloqueio temporário do usuário por senhas erradas seguidas.</summary>
public sealed class LoginOptions
{
    public const string SectionName = "Login";

    public int MaxTentativas { get; set; } = 5;

    public int BloqueioMinutos { get; set; } = 15;
}
