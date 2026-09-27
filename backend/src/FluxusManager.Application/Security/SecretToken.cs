using System.Security.Cryptography;
using System.Text;

namespace FluxusManager.Application.Security;

/// <summary>
/// Credencial opaca (refresh token, link de convite): 256 bits aleatórios em hexadecimal.
/// Apenas o SHA-256 é persistido, nunca a credencial original.
/// </summary>
public static class SecretToken
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
