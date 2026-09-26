namespace FluxusManager.Domain.Entities;

/// <summary>
/// Entidade que pertence a um tenant (empresa). Consultas são filtradas pelo tenant da requisição
/// e o <see cref="TenantId"/> é preenchido automaticamente ao incluir.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
