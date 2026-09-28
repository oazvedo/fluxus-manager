namespace FluxusManager.Domain.Entities;

/// <summary>Token de uso único para redefinir a senha de um usuário.</summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? UsadoEm { get; private set; }

    public PasswordResetToken(Guid usuarioId, string tokenHash, DateTime expiraEm)
    {
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        ExpiraEm = expiraEm;
    }

    public void Usar(DateTime agora) => UsadoEm ??= agora;
}
