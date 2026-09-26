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

    public bool Verificar(string senha, string senhaHash)
        => _hasher.VerifyHashedPassword(SemUsuario, senhaHash, senha) != PasswordVerificationResult.Failed;
}
