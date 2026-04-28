using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class addsefazvalidationfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FonteValidacao",
                table: "NotaFiscal",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusSefaz",
                table: "NotaFiscal",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaValidacaoSefaz",
                table: "NotaFiscal",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FonteValidacao",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "StatusSefaz",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "UltimaValidacaoSefaz",
                table: "NotaFiscal");
        }
    }
}
