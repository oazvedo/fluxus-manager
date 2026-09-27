namespace FluxusManager.Domain.Entities;

/// <summary>Permissão do catálogo concedida por um perfil.</summary>
public class PerfilPermissao : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid PerfilId { get; private set; }
    public string Codigo { get; private set; }

    public Perfil Perfil { get; private set; } = null!;

    public PerfilPermissao(Guid tenantId, Guid perfilId, string codigo)
    {
        TenantId = tenantId;
        PerfilId = perfilId;
        Codigo = codigo;
    }
}
