namespace FluxusManager.Application.Options;

public class RefreshTokenOptions
{
    public const string SectionName = "RefreshTokens";
    public int DuracaoDias { get; set; } = 7;
    public bool LimpezaHabilitada { get; set; } = true;
}
