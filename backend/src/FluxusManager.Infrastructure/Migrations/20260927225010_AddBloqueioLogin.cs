using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxusManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBloqueioLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bloqueado_ate",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tentativas_login_falhas",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ultima_falha_login_em",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bloqueado_ate",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "tentativas_login_falhas",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ultima_falha_login_em",
                table: "usuarios");
        }
    }
}
