using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using FluxusManager.Infrastructure.Database;

namespace FluxusManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFiliais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filiais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: false),
                    endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filiais", x => x.id);
                    table.ForeignKey(
                        name: "fk_filiais_empresas_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.EnableAuditTracking("filiais");

            migrationBuilder.CreateIndex(
                name: "ix_filiais_cnpj",
                table: "filiais",
                column: "cnpj",
                unique: true,
                filter: "excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_filiais_tenant_id",
                table: "filiais",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filiais");
        }
    }
}
