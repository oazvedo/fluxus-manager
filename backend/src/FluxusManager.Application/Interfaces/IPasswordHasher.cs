namespace FluxusManager.Application.Interfaces;

/// <summary>Gera e confere hash de senha. A implementação fica na Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string senha);

    bool Verificar(string senha, string senhaHash);
}
