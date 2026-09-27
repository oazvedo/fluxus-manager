namespace FluxusManager.Domain.Entities;

/// <summary>
/// Base de todas as entidades. As datas são preenchidas automaticamente ao salvar.
/// Remover uma entidade é uma exclusão lógica: <see cref="Excluido"/> vira true e ela some das consultas.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    public bool Excluido { get; private set; }
}
