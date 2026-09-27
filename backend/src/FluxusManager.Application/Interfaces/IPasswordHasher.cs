namespace FluxusManager.Application.Interfaces;

/// <summary>Gera e confere hash de senha. A implementação fica na Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string senha);

    bool Verificar(string senha, string senhaHash);

    /// <summary>
    /// Verificação com o mesmo custo de <see cref="Verificar"/>, contra um hash fictício, para quando não há usuário
    /// a conferir: o tempo de resposta não revela se o e-mail existe.
    /// </summary>
    void VerificarSemUsuario(string senha);
}
