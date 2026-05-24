using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class adddispositivousuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptCheckedAt",
                table: "NotificacaoEntrega",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DispositivoUsuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpoPushToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Plataforma = table.Column<int>(type: "integer", nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UltimoUsoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositivoUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispositivoUsuario_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoEntrega_Receipt_Pending",
                table: "NotificacaoEntrega",
                column: "UltimaTentativaEm",
                filter: "\"Status\" = 1 AND \"Canal\" = 2 AND \"ReceiptCheckedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DispositivoUsuario_ExpoPushToken",
                table: "DispositivoUsuario",
                column: "ExpoPushToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispositivoUsuario_UsuarioAtivo",
                table: "DispositivoUsuario",
                column: "UsuarioId",
                filter: "\"Ativo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispositivoUsuario");

            migrationBuilder.DropIndex(
                name: "IX_NotificacaoEntrega_Receipt_Pending",
                table: "NotificacaoEntrega");

            migrationBuilder.DropColumn(
                name: "ReceiptCheckedAt",
                table: "NotificacaoEntrega");
        }
    }
}
