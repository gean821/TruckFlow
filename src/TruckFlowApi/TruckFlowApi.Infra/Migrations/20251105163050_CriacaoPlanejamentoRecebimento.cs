using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoPlanejamentoRecebimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanejamentoRecebimento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusRecebimento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanejamentoRecebimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanejamentoRecebimento_Fornecedor_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemPlanejamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanejamentoRecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadeTotalPlanejada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CadenciaDiariaPlanejada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantidadeTotalRecebida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPlanejamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPlanejamento_PlanejamentoRecebimento_PlanejamentoRecebimentoId",
                        column: x => x.PlanejamentoRecebimentoId,
                        principalTable: "PlanejamentoRecebimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPlanejamento_Produto_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPlanejamento_PlanejamentoRecebimentoId",
                table: "ItemPlanejamento",
                column: "PlanejamentoRecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPlanejamento_ProdutoId",
                table: "ItemPlanejamento",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanejamentoRecebimento_FornecedorId",
                table: "PlanejamentoRecebimento",
                column: "FornecedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemPlanejamento");

            migrationBuilder.DropTable(
                name: "PlanejamentoRecebimento");
        }
    }
}
