using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AgendamentoHorariosCiclo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChamadoDocaEm",
                table: "Agendamento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChegouEm",
                table: "Agendamento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FimDescargaEm",
                table: "Agendamento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InicioDescargaEm",
                table: "Agendamento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaiuEm",
                table: "Agendamento",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChamadoDocaEm",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "ChegouEm",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "FimDescargaEm",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "InicioDescargaEm",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "SaiuEm",
                table: "Agendamento");
        }
    }
}
