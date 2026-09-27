namespace FluxusManager.Application.Security;

public static class ExpiracaoExtensions
{
    /// <summary>
    /// Agora + <paramref name="validade"/>, em UTC e com precisão de segundos: a resposta mostra a mesma expiração
    /// que fica gravada no PostgreSQL.
    /// </summary>
    public static DateTime ExpiracaoEmSegundos(this TimeProvider clock, TimeSpan validade)
        => DateTimeOffset.FromUnixTimeSeconds(clock.GetUtcNow().Add(validade).ToUnixTimeSeconds()).UtcDateTime;
}
