namespace FluxusManager.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public bool Ativo { get; private set; } = true;

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

    public void Inativar() => Ativo = false;
}
