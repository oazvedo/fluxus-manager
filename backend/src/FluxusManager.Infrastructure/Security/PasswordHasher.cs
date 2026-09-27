using FluxusManager.Application.Interfaces;
using IdentityHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;
using PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace FluxusManager.Infrastructure.Security;

/// <summary>
/// Hash de senha com o algoritmo do ASP.NET Core Identity (PBKDF2 com salt aleatório).
/// O hash gerado já carrega a versão do algoritmo e o salt, por isso é guardado numa única coluna.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private static readonly object SemUsuario = new();

    private readonly IdentityHasher _hasher = new();

    public string Hash(string senha) => _hasher.HashPassword(SemUsuario, senha);

    // Mesmos parâmetros do algoritmo dos hashes reais, calculado uma vez por processo.
    private static readonly Lazy<string> HashFicticio = new(() => new IdentityHasher().HashPassword(SemUsuario, Guid.NewGuid().ToString()));

    public bool Verificar(string senha, string senhaHash)
        => _hasher.VerifyHashedPassword(SemUsuario, senhaHash, senha) != PasswordVerificationResult.Failed;

    public void VerificarSemUsuario(string senha) => Verificar(senha, HashFicticio.Value);
}
