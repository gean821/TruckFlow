using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class bancodnv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataFim",
                table: "PlanejamentoRecebimento",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DiasSemana",
                table: "ItemPlanejamento",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ToleranciaExtra",
                table: "ItemPlanejamento",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 30m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFim",
                table: "PlanejamentoRecebimento");

            migrationBuilder.DropColumn(
                name: "DiasSemana",
                table: "ItemPlanejamento");

            migrationBuilder.DropColumn(
                name: "ToleranciaExtra",
                table: "ItemPlanejamento");
        }
    }
}
