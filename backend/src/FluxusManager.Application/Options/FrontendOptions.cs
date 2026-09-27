namespace FluxusManager.Application.Options;

/// <summary>Endereço público do frontend, usado para montar os links enviados por e-mail.</summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>URL absoluta HTTP(S), sem barra final (ex.: <c>https://app.exemplo.com</c>). Vem da configuração, nunca da requisição.</summary>
    public string Url { get; set; } = string.Empty;

    public string Link(string caminho) => $"{Url.TrimEnd('/')}/{caminho.TrimStart('/')}";
}
