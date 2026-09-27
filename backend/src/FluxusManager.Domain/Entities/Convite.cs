namespace FluxusManager.Domain.Entities;

/// <summary>
/// Convite por e-mail para acessar uma empresa com um perfil. Guarda só o hash do token enviado.
/// A expiração não é um status gravado: um convite pendente vence quando passa de <see cref="ExpiraEm"/>.
/// </summary>
public class Convite : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public Guid PerfilId { get; private set; }
    public Perfil Perfil { get; private set; } = null!;
    public string TokenHash { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public ConviteStatus Status { get; private set; } = ConviteStatus.Pendente;

    /// <summary>Usuário que aceitou o convite (existente ou criado no aceite).</summary>
    public Guid? UsuarioId { get; private set; }

    public DateTime? AceitoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }

    public Convite(Perfil perfil, string email, string tokenHash, DateTime expiraEm)
    {
        TenantId = perfil.TenantId;
        Perfil = perfil;
        PerfilId = perfil.Id;
        Email = email;
        TokenHash = tokenHash;
        ExpiraEm = expiraEm;
    }

    // Construtor do EF.
    private Convite()
    {
        Email = null!;
        TokenHash = null!;
    }

    public bool Pendente(DateTime agora) => Status == ConviteStatus.Pendente && ExpiraEm > agora;

    public bool Expirado(DateTime agora) => Status == ConviteStatus.Pendente && ExpiraEm <= agora;

    /// <summary>Troca o token (o link anterior deixa de valer) e renova o prazo.</summary>
    public void Renovar(string tokenHash, DateTime expiraEm)
    {
        GarantirPendente();
        TokenHash = tokenHash;
        ExpiraEm = expiraEm;
    }

    public void Aceitar(Guid usuarioId, DateTime agora)
    {
        if (!Pendente(agora))
            throw new InvalidOperationException("Só um convite pendente e dentro do prazo pode ser aceito.");
        Status = ConviteStatus.Aceito;
        UsuarioId = usuarioId;
        AceitoEm = agora;
    }

    public void Cancelar(DateTime agora)
    {
        GarantirPendente();
        Status = ConviteStatus.Cancelado;
        CanceladoEm = agora;
    }

    private void GarantirPendente()
    {
        if (Status != ConviteStatus.Pendente)
            throw new InvalidOperationException($"Operação exige convite pendente, mas ele está {Status}.");
    }
}

public enum ConviteStatus
{
    Pendente,
    Aceito,
    Cancelado
}
