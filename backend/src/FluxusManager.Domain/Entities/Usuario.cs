namespace FluxusManager.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public bool Ativo { get; private set; } = true;

    /// <summary>
    /// Senhas erradas seguidas dentro da janela de bloqueio. Incrementada de forma atômica no banco
    /// (<c>IUsuarioRepository.RegistrarFalhaLoginAsync</c>), para tentativas simultâneas não se perderem.
    /// </summary>
    public int TentativasLoginFalhas { get; private set; }

    public DateTime? UltimaFalhaLoginEm { get; private set; }

    /// <summary>Enquanto no futuro, o login é recusado mesmo com a senha certa.</summary>
    public DateTime? BloqueadoAte { get; private set; }

    public Usuario(string nome, string email, string senhaHash)
    {
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
    }

    public void Atualizar(string nome, string email)
    {
        Nome = nome;
        Email = email;
    }

    public void Ativar() => Ativo = true;

    public void AlterarSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        Desbloquear();
    }

    public void Inativar() => Ativo = false;

    public bool Bloqueado(DateTime agora) => BloqueadoAte > agora;

    /// <summary>Login concluído ou desbloqueio pelo administrador: zera a contagem e o bloqueio.</summary>
    public void Desbloquear()
    {
        TentativasLoginFalhas = 0;
        UltimaFalhaLoginEm = null;
        BloqueadoAte = null;
    }
}
