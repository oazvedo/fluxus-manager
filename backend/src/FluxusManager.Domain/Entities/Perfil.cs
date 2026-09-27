namespace FluxusManager.Domain.Entities;

/// <summary>Conjunto de permissões administrado por uma empresa.</summary>
public class Perfil : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Nome { get; private set; }
    public string NomeNormalizado { get; private set; }
    public string? Descricao { get; private set; }
    public bool Ativo { get; private set; } = true;
    public ICollection<PerfilPermissao> Permissoes { get; private set; } = new List<PerfilPermissao>();
    public ICollection<UsuarioEmpresa> Vinculos { get; private set; } = new List<UsuarioEmpresa>();

    public Perfil(Guid tenantId, string nome, string? descricao)
    {
        TenantId = tenantId;
        Nome = nome;
        NomeNormalizado = Normalizar(nome);
        Descricao = descricao;
    }

    public void Atualizar(string nome, string? descricao, IEnumerable<string> permissoes)
    {
        Nome = nome;
        NomeNormalizado = Normalizar(nome);
        Descricao = descricao;
        Permissoes.Clear();
        foreach (var permissao in permissoes.Distinct(StringComparer.Ordinal))
            Permissoes.Add(new PerfilPermissao(TenantId, Id, permissao));
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;

    private static string Normalizar(string nome) => nome.Trim().ToUpperInvariant();

    public static Perfil CriarPadrao(Guid empresaId, string nome, string descricao, IEnumerable<string> permissoes)
    {
        var perfil = new Perfil(empresaId, nome, descricao);
        foreach (var permissao in permissoes.Distinct(StringComparer.Ordinal))
            perfil.Permissoes.Add(new PerfilPermissao(empresaId, perfil.Id, permissao));
        return perfil;
    }
}
