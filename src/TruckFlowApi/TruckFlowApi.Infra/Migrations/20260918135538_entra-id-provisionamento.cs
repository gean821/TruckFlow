using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class entraidprovisionamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "Usuario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoSyncEntraEm",
                table: "Usuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Configuracoes",
                table: "Empresa",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntraGroupRoleMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntraGroupId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntraGroupNome = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntraGroupRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntraGroupRoleMapping_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntraGroupRoleMapping_EmpresaId",
                table: "EntraGroupRoleMapping",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntraGroupRoleMapping_EntraGroupId",
                table: "EntraGroupRoleMapping",
                column: "EntraGroupId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntraGroupRoleMapping");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UltimoSyncEntraEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "Configuracoes",
                table: "Empresa");
        }
    }
}
