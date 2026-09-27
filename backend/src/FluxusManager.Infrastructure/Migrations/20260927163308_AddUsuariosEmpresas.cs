using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using FluxusManager.Infrastructure.Database;

namespace FluxusManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuariosEmpresas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios_empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios_empresas", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_empresas_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuarios_empresas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.EnableAuditTracking("usuarios_empresas");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_empresas_empresa_id",
                table: "usuarios_empresas",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_empresas_usuario_id_empresa_id",
                table: "usuarios_empresas",
                columns: new[] { "usuario_id", "empresa_id" },
                unique: true,
                filter: "excluido = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios_empresas");
        }
    }
}
