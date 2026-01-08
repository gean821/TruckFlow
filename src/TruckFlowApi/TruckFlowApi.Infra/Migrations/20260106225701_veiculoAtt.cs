using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class veiculoAtt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_MotoristaId",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "VeiculoId",
                table: "Motorista");

            migrationBuilder.AddColumn<string>(
                name: "PlacaVeiculo",
                table: "Agendamento",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoVeiculo",
                table: "Agendamento",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_MotoristaId",
                table: "Veiculo",
                column: "MotoristaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_MotoristaId",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "PlacaVeiculo",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "TipoVeiculo",
                table: "Agendamento");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Veiculo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VeiculoId",
                table: "Motorista",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_MotoristaId",
                table: "Veiculo",
                column: "MotoristaId",
                unique: true);
        }
    }
}
