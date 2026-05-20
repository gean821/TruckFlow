using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class addprodutofornecedormapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutoFornecedor_FornecedorId",
                table: "ProdutoFornecedor");

            migrationBuilder.AddColumn<string>(
                name: "CodigoFornecedor",
                table: "ProdutoFornecedor",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EanFornecedor",
                table: "ProdutoFornecedor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFornecedor_FornecedorId_CodigoFornecedor",
                table: "ProdutoFornecedor",
                columns: new[] { "FornecedorId", "CodigoFornecedor" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutoFornecedor_FornecedorId_CodigoFornecedor",
                table: "ProdutoFornecedor");

            migrationBuilder.DropColumn(
                name: "CodigoFornecedor",
                table: "ProdutoFornecedor");

            migrationBuilder.DropColumn(
                name: "EanFornecedor",
                table: "ProdutoFornecedor");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFornecedor_FornecedorId",
                table: "ProdutoFornecedor",
                column: "FornecedorId");
        }
    }
}
