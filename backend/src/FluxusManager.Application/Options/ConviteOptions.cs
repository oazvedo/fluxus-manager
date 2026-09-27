namespace FluxusManager.Application.Options;

/// <summary>Prazo dos convites de acesso.</summary>
public sealed class ConviteOptions
{
    public const string SectionName = "Convites";

    /// <summary>Horas até o link do convite vencer, contadas do envio ou do último reenvio.</summary>
    public int ValidadeHoras { get; set; } = 72;
}
