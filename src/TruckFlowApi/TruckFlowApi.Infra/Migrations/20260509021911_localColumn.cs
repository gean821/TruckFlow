using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class localColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocalDescargaId",
                table: "Agendamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_LocalDescargaId_DataInicio_DataFim",
                table: "Agendamento",
                columns: new[] { "LocalDescargaId", "DataInicio", "DataFim" });

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamento_LocalDescarga_LocalDescargaId",
                table: "Agendamento",
                column: "LocalDescargaId",
                principalTable: "LocalDescarga",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamento_LocalDescarga_LocalDescargaId",
                table: "Agendamento");

            migrationBuilder.DropIndex(
                name: "IX_Agendamento_LocalDescargaId_DataInicio_DataFim",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "LocalDescargaId",
                table: "Agendamento");
        }
    }
}
