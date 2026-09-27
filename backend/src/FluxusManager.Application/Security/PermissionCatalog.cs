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

    public static IReadOnlyCollection<string> All { get; } = Array.AsReadOnly<string>(
    [
        EmpresasVisualizar, EmpresasEditar, FiliaisVisualizar, FiliaisEditar,
        UsuariosVisualizar, UsuariosEditar, UsuariosEmpresasVisualizar, UsuariosEmpresasEditar
    ]);

    public static IReadOnlyCollection<string> ReadOnly { get; } = Array.AsReadOnly<string>(
    [EmpresasVisualizar, FiliaisVisualizar, UsuariosVisualizar, UsuariosEmpresasVisualizar]);
}
