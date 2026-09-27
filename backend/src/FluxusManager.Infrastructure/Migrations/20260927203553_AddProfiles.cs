using System;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxusManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "perfil",
                table: "usuarios_empresas",
                newName: "perfil_legado");

            migrationBuilder.CreateTable(
                name: "perfis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nome_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_perfis", x => x.id);
                    table.UniqueConstraint("ak_perfis_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_perfis_empresas_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_permissoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_perfil_permissoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_perfil_permissoes_perfis_perfil_id_tenant_id",
                        columns: x => new { x.perfil_id, x.tenant_id },
                        principalTable: "perfis",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.EnableAuditTracking("perfis");
            migrationBuilder.EnableAuditTracking("perfil_permissoes");

            migrationBuilder.AddColumn<Guid>(
                name: "perfil_id",
                table: "usuarios_empresas",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO perfis (id, tenant_id, nome, nome_normalizado, descricao, ativo, criado_em, excluido)
                SELECT gen_random_uuid(), id, 'Administrador', 'ADMINISTRADOR', 'Acesso completo à empresa.', true, now(), false FROM empresas
                UNION ALL
                SELECT gen_random_uuid(), id, 'Consulta', 'CONSULTA', 'Acesso de leitura aos cadastros.', true, now(), false FROM empresas;

                INSERT INTO perfil_permissoes (id, tenant_id, perfil_id, codigo, criado_em, excluido)
                SELECT gen_random_uuid(), perfil.tenant_id, perfil.id, permissao.codigo, now(), false
                FROM perfis perfil
                CROSS JOIN (VALUES
                    ('empresas.visualizar'), ('empresas.editar'), ('filiais.visualizar'), ('filiais.editar'),
                    ('usuarios.visualizar'), ('usuarios.editar'), ('usuarios-empresas.visualizar'),
                    ('usuarios-empresas.editar'), ('perfis.visualizar'), ('perfis.editar')
                ) AS permissao(codigo)
                WHERE perfil.nome_normalizado = 'ADMINISTRADOR';

                INSERT INTO perfil_permissoes (id, tenant_id, perfil_id, codigo, criado_em, excluido)
                SELECT gen_random_uuid(), perfil.tenant_id, perfil.id, permissao.codigo, now(), false
                FROM perfis perfil
                CROSS JOIN (VALUES
                    ('empresas.visualizar'), ('filiais.visualizar'), ('usuarios.visualizar'),
                    ('usuarios-empresas.visualizar'), ('perfis.visualizar')
                ) AS permissao(codigo)
                WHERE perfil.nome_normalizado = 'CONSULTA';

                INSERT INTO perfis (id, tenant_id, nome, nome_normalizado, descricao, ativo, criado_em, excluido)
                SELECT DISTINCT ON (vinculo.empresa_id, upper(btrim(vinculo.perfil_legado)))
                    gen_random_uuid(), vinculo.empresa_id, btrim(vinculo.perfil_legado), upper(btrim(vinculo.perfil_legado)),
                    'Perfil legado sem permissões adicionais.', true, now(), false
                FROM usuarios_empresas vinculo
                WHERE lower(btrim(vinculo.perfil_legado)) NOT IN ('administrador', 'admin', 'consulta', 'leitura', 'read-only')
                    AND btrim(vinculo.perfil_legado) <> ''
                ORDER BY vinculo.empresa_id, upper(btrim(vinculo.perfil_legado)), btrim(vinculo.perfil_legado);

                UPDATE usuarios_empresas vinculo
                SET perfil_id = perfil.id
                FROM perfis perfil
                WHERE perfil.tenant_id = vinculo.empresa_id
                    AND perfil.nome_normalizado = CASE lower(btrim(vinculo.perfil_legado))
                        WHEN 'admin' THEN 'ADMINISTRADOR'
                        WHEN 'administrador' THEN 'ADMINISTRADOR'
                        WHEN 'consulta' THEN 'CONSULTA'
                        WHEN 'leitura' THEN 'CONSULTA'
                        WHEN 'read-only' THEN 'CONSULTA'
                        WHEN '' THEN 'CONSULTA'
                        ELSE upper(btrim(vinculo.perfil_legado))
                    END;
                """);

            migrationBuilder.AlterColumn<Guid>(name: "perfil_id", table: "usuarios_empresas", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.DropColumn(name: "perfil_legado", table: "usuarios_empresas");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_empresas_perfil_id_empresa_id",
                table: "usuarios_empresas",
                columns: new[] { "perfil_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_perfil_permissoes_perfil_id_tenant_id",
                table: "perfil_permissoes",
                columns: new[] { "perfil_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_perfil_permissoes_tenant_id_perfil_id_codigo",
                table: "perfil_permissoes",
                columns: new[] { "tenant_id", "perfil_id", "codigo" },
                unique: true,
                filter: "excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_perfis_tenant_id_nome_normalizado",
                table: "perfis",
                columns: new[] { "tenant_id", "nome_normalizado" },
                unique: true,
                filter: "excluido = false");

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_empresas_perfis_perfil_id_empresa_id",
                table: "usuarios_empresas",
                columns: new[] { "perfil_id", "empresa_id" },
                principalTable: "perfis",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_empresas_perfis_perfil_id_empresa_id",
                table: "usuarios_empresas");

            migrationBuilder.AddColumn<string>(name: "perfil", table: "usuarios_empresas", type: "character varying(50)", maxLength: 50, nullable: true);
            migrationBuilder.Sql("UPDATE usuarios_empresas vinculo SET perfil = perfil.nome FROM perfis perfil WHERE perfil.id = vinculo.perfil_id");
            migrationBuilder.AlterColumn<string>(name: "perfil", table: "usuarios_empresas", type: "character varying(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50, oldNullable: true);

            migrationBuilder.DropTable(
                name: "perfil_permissoes");

            migrationBuilder.DropTable(
                name: "perfis");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_empresas_perfil_id_empresa_id",
                table: "usuarios_empresas");

            migrationBuilder.DropColumn(
                name: "perfil_id",
                table: "usuarios_empresas");

        }
    }
}
