namespace FluxusManager.Domain.Entities;

/// <summary>
/// Base de todas as entidades. As datas são preenchidas automaticamente ao salvar.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }
}
