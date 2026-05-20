using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class nfeItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ean",
                table: "NotaFiscalItem",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MatchadoEm",
                table: "NotaFiscalItem",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MatchadoPor",
                table: "NotaFiscalItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemMatch",
                table: "NotaFiscalItem",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NotaFiscalItem",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItem_EmpresaId_Status",
                table: "NotaFiscalItem",
                columns: new[] { "EmpresaId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotaFiscalItem_EmpresaId_Status",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "Ean",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "MatchadoEm",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "MatchadoPor",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "OrigemMatch",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotaFiscalItem");
        }
    }
}
