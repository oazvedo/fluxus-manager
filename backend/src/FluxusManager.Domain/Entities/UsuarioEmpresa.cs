namespace FluxusManager.Domain.Entities;

/// <summary>Vínculo de acesso de um usuário a uma empresa.</summary>
public class UsuarioEmpresa : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Perfil { get; private set; }
    public bool Ativo { get; private set; } = true;

    public UsuarioEmpresa(Guid usuarioId, Guid empresaId, string perfil)
    {
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        Perfil = perfil;
    }

    public void AtualizarPerfil(string perfil) => Perfil = perfil;
    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
}
