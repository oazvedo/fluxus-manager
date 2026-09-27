using Microsoft.EntityFrameworkCore.Migrations;

namespace FluxusManager.Infrastructure.Database;

/// <summary>
/// Liga a auditoria por trigger numa tabela. Convenção: toda migration que cria tabela chama
/// <see cref="EnableAuditTracking"/> logo depois do CreateTable (o teste AuditoriaTests confere).
/// </summary>
public static class AuditMigrationExtensions
{
    /// <param name="table">Tabela no schema public, ex.: "usuarios".</param>
    /// <param name="ignoredColumns">
    /// Colunas sensíveis (ex.: "senha_hash"): a alteração aparece em changed_fields, mas o valor nunca é gravado.
    /// </param>
    public static void EnableAuditTracking(this MigrationBuilder migrationBuilder, string table, params string[] ignoredColumns)
    {
        var ignored = ignoredColumns.Length == 0
            ? "ARRAY[]::text[]"
            : $"ARRAY[{string.Join(", ", ignoredColumns.Select(c => $"'{c}'"))}]";

        migrationBuilder.Sql($"SELECT audit.enable_tracking('public.{table}', {ignored});");
    }

    public static void DisableAuditTracking(this MigrationBuilder migrationBuilder, string table)
        => migrationBuilder.Sql($"SELECT audit.disable_tracking('public.{table}');");
}
