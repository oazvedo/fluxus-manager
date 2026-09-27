namespace FluxusManager.Domain.Entities;

/// <summary>Credencial de renovação: guarda somente o hash. A família mantém a expiração absoluta do login.</summary>
public class RefreshToken : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid FamiliaId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? RevogadoEm { get; private set; }
    public Guid? SubstituidoPorId { get; private set; }

    public RefreshToken(Guid usuarioId, Guid empresaId, Guid familiaId, string tokenHash, DateTime expiraEm)
    {
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        FamiliaId = familiaId;
        TokenHash = tokenHash;
        ExpiraEm = expiraEm;
    }

    public void Revogar(DateTime agora) => RevogadoEm ??= agora;

    public void Substituir(Guid substitutoId, DateTime agora)
    {
        Revogar(agora);
        SubstituidoPorId = substitutoId;
    }
}
