namespace FluxusManager.Application.Options;

/// <summary>Regras configuráveis do cadastro de filiais.</summary>
public sealed class FilialOptions
{
    public const string SectionName = "Filiais";

    /// <summary>Exige que os oito primeiros caracteres do CNPJ da filial coincidam com os da empresa.</summary>
    public bool ValidarRaizCnpjDaEmpresa { get; set; } = true;
}
