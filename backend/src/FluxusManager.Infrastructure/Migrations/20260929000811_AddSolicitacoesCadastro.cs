using System;
using Microsoft.EntityFrameworkCore.Migrations;
using FluxusManager.Infrastructure.Database;

#nullable disable

namespace FluxusManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitacoesCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administradores_plataforma",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administradores_plataforma", x => x.id);
                    table.ForeignKey(
                        name: "fk_administradores_plataforma_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitacoes_cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: false),
                    responsavel_nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    responsavel_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    responsavel_telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verificacao_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    verificacao_expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acompanhamento_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    acompanhamento_expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verificada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decidida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decidida_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observacao_interna = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    motivo_recusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitacoes_cadastro", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_usuario_decidida_por_id",
                        column: x => x.decidida_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitacoes_cadastro_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    ultima_tentativa_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    proxima_tentativa_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    convite_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitacoes_cadastro_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_emails_convites_convite_id",
                        column: x => x.convite_id,
                        principalTable: "convites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_emails_solicitacoes_cadastro_solicita",
                        column: x => x.solicitacao_id,
                        principalTable: "solicitacoes_cadastro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "solicitacoes_cadastro_eventos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitacoes_cadastro_eventos", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_eventos_solicitacoes_cadastro_solicit",
                        column: x => x.solicitacao_id,
                        principalTable: "solicitacoes_cadastro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_solicitacoes_cadastro_eventos_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Os hashes dos links nunca vão para a auditoria; a alteração deles continua registrada em changed_fields.
            migrationBuilder.EnableAuditTracking("administradores_plataforma");
            migrationBuilder.EnableAuditTracking("solicitacoes_cadastro", "verificacao_token_hash", "acompanhamento_token_hash");
            migrationBuilder.EnableAuditTracking("solicitacoes_cadastro_emails");
            migrationBuilder.EnableAuditTracking("solicitacoes_cadastro_eventos");

            migrationBuilder.CreateIndex(
                name: "ix_administradores_plataforma_usuario_id",
                table: "administradores_plataforma",
                column: "usuario_id",
                unique: true,
                filter: "excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_acompanhamento_token_hash",
                table: "solicitacoes_cadastro",
                column: "acompanhamento_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_cnpj",
                table: "solicitacoes_cadastro",
                column: "cnpj",
                unique: true,
                filter: "status IN ('AguardandoVerificacao', 'PendenteAnalise') AND excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_decidida_por_id",
                table: "solicitacoes_cadastro",
                column: "decidida_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_empresa_id",
                table: "solicitacoes_cadastro",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_responsavel_email",
                table: "solicitacoes_cadastro",
                column: "responsavel_email",
                unique: true,
                filter: "status IN ('AguardandoVerificacao', 'PendenteAnalise') AND excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_status_criado_em",
                table: "solicitacoes_cadastro",
                columns: new[] { "status", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_verificacao_token_hash",
                table: "solicitacoes_cadastro",
                column: "verificacao_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_emails_convite_id",
                table: "solicitacoes_cadastro_emails",
                column: "convite_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_emails_solicitacao_id_tipo",
                table: "solicitacoes_cadastro_emails",
                columns: new[] { "solicitacao_id", "tipo" },
                unique: true,
                filter: "excluido = false");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_emails_status_proxima_tentativa_em",
                table: "solicitacoes_cadastro_emails",
                columns: new[] { "status", "proxima_tentativa_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_eventos_solicitacao_id_criado_em",
                table: "solicitacoes_cadastro_eventos",
                columns: new[] { "solicitacao_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_cadastro_eventos_usuario_id",
                table: "solicitacoes_cadastro_eventos",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administradores_plataforma");

            migrationBuilder.DropTable(
                name: "solicitacoes_cadastro_emails");

            migrationBuilder.DropTable(
                name: "solicitacoes_cadastro_eventos");

            migrationBuilder.DropTable(
                name: "solicitacoes_cadastro");
        }
    }
}
