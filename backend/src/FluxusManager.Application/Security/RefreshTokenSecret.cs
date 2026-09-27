using System.Security.Cryptography;
using System.Text;

namespace FluxusManager.Application.Security;

/// <summary>256 bits aleatórios; apenas o SHA-256 é persistido, nunca a credencial original.</summary>
public static class RefreshTokenSecret
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
