using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class editNotaFornecedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotaFiscal_FornecedorId",
                table: "NotaFiscal");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscal_FornecedorId",
                table: "NotaFiscal",
                column: "FornecedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotaFiscal_FornecedorId",
                table: "NotaFiscal");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscal_FornecedorId",
                table: "NotaFiscal",
                column: "FornecedorId",
                unique: true);
        }
    }
}
