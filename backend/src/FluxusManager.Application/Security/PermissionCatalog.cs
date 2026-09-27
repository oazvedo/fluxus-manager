namespace FluxusManager.Application.Security;

/// <summary>Nomes de permissões emitidos nos tokens e usados pelas políticas da API.</summary>
public static class PermissionCatalog
{
    public const string EmpresasVisualizar = "empresas.visualizar";
    public const string EmpresasEditar = "empresas.editar";
    public const string FiliaisVisualizar = "filiais.visualizar";
    public const string FiliaisEditar = "filiais.editar";
    public const string UsuariosVisualizar = "usuarios.visualizar";
    public const string UsuariosEditar = "usuarios.editar";
    public const string UsuariosEmpresasVisualizar = "usuarios-empresas.visualizar";
    public const string UsuariosEmpresasEditar = "usuarios-empresas.editar";
    public const string PerfisVisualizar = "perfis.visualizar";
    public const string PerfisEditar = "perfis.editar";

    public static IReadOnlyCollection<string> All { get; } = Array.AsReadOnly<string>(
    [
        EmpresasVisualizar, EmpresasEditar, FiliaisVisualizar, FiliaisEditar,
        UsuariosVisualizar, UsuariosEditar, UsuariosEmpresasVisualizar, UsuariosEmpresasEditar,
        PerfisVisualizar, PerfisEditar
    ]);

    public static IReadOnlyCollection<string> ReadOnly { get; } = Array.AsReadOnly<string>(
        [EmpresasVisualizar, FiliaisVisualizar, UsuariosVisualizar, UsuariosEmpresasVisualizar, PerfisVisualizar]);
}

public sealed record PermissaoCatalogoItem(string Codigo, string Nome, string Descricao);

public static class PermissaoCatalogo
{
    public static IReadOnlyList<PermissaoCatalogoItem> Todos { get; } = Array.AsReadOnly<PermissaoCatalogoItem>(
    [
        new(PermissionCatalog.EmpresasVisualizar, "Visualizar empresas", "Consultar os dados das empresas."),
        new(PermissionCatalog.EmpresasEditar, "Editar empresas", "Cadastrar, editar e alterar o status de empresas."),
        new(PermissionCatalog.FiliaisVisualizar, "Visualizar filiais", "Consultar os dados das filiais."),
        new(PermissionCatalog.FiliaisEditar, "Editar filiais", "Cadastrar, editar e alterar o status de filiais."),
        new(PermissionCatalog.UsuariosVisualizar, "Visualizar usuários", "Consultar os dados dos usuários."),
        new(PermissionCatalog.UsuariosEditar, "Editar usuários", "Cadastrar, editar e alterar o status de usuários."),
        new(PermissionCatalog.UsuariosEmpresasVisualizar, "Visualizar vínculos", "Consultar os vínculos de usuários e empresas."),
        new(PermissionCatalog.UsuariosEmpresasEditar, "Editar vínculos", "Criar, editar e remover vínculos de usuários."),
        new(PermissionCatalog.PerfisVisualizar, "Visualizar perfis", "Consultar perfis e suas permissões."),
        new(PermissionCatalog.PerfisEditar, "Editar perfis", "Criar, editar e remover perfis da empresa.")
    ]);

    public static IReadOnlySet<string> Codigos { get; } = new HashSet<string>(Todos.Select(permissao => permissao.Codigo), StringComparer.Ordinal);
}
