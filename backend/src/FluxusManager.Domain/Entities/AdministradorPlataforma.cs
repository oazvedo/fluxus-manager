namespace FluxusManager.Domain.Entities;

/// <summary>
/// Papel global do Fluxus, separado dos perfis e permissões de qualquer empresa. Só é concedido ou revogado pelo
/// comando de linha de comando da API (ou pelo seed local); nenhum endpoint o altera.
/// </summary>
public class AdministradorPlataforma : BaseEntity
{
    public Guid UsuarioId { get; private set; }

    public AdministradorPlataforma(Guid usuarioId) => UsuarioId = usuarioId;
}
