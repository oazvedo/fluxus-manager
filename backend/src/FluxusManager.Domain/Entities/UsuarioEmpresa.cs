namespace FluxusManager.Domain.Entities;

/// <summary>Vínculo de acesso de um usuário a uma empresa.</summary>
public class UsuarioEmpresa : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid PerfilId { get; private set; }
    public Perfil Perfil { get; private set; } = null!;
    public bool Ativo { get; private set; } = true;

    public UsuarioEmpresa(Guid usuarioId, Guid empresaId, Guid perfilId)
    {
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        PerfilId = perfilId;
    }

    public void AssociarPerfil(Perfil perfil)
    {
        if (perfil.TenantId != EmpresaId)
            throw new ArgumentException("O perfil deve pertencer à mesma empresa do vínculo.", nameof(perfil));
        Perfil = perfil;
        PerfilId = perfil.Id;
    }

    public void AtualizarPerfil(Perfil perfil) => AssociarPerfil(perfil);
    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
}
