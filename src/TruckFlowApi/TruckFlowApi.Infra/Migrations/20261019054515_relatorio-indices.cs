using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class relatorioindices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamento_EmpresaId",
                table: "Agendamento");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscal_PlacaVeiculo_trgm",
                table: "NotaFiscal",
                column: "PlacaVeiculo")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Motorista_NomeReal_trgm",
                table: "Motorista",
                column: "NomeReal")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_EmpresaId_DataInicio",
                table: "Agendamento",
                columns: new[] { "EmpresaId", "DataInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_PlacaVeiculo_trgm",
                table: "Agendamento",
                column: "PlacaVeiculo")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotaFiscal_PlacaVeiculo_trgm",
                table: "NotaFiscal");

            migrationBuilder.DropIndex(
                name: "IX_Motorista_NomeReal_trgm",
                table: "Motorista");

            migrationBuilder.DropIndex(
                name: "IX_Agendamento_EmpresaId_DataInicio",
                table: "Agendamento");

            migrationBuilder.DropIndex(
                name: "IX_Agendamento_PlacaVeiculo_trgm",
                table: "Agendamento");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_EmpresaId",
                table: "Agendamento",
                column: "EmpresaId");
        }
    }
}
