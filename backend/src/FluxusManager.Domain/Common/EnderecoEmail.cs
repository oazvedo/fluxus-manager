namespace FluxusManager.Domain.Common;

/// <summary>E-mails são gravados e comparados sempre normalizados (sem espaços, minúsculos).</summary>
public static class EnderecoEmail
{
    public static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
