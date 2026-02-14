using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class eventoRecebimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "Agendamento",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecebimentoEvento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemPlanejamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataRecebimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecebimentoEvento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecebimentoEvento_Agendamento_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecebimentoEvento_ItemPlanejamento_ItemPlanejamentoId",
                        column: x => x.ItemPlanejamentoId,
                        principalTable: "ItemPlanejamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_ProdutoId",
                table: "Agendamento",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_AgendamentoId",
                table: "RecebimentoEvento",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_ItemPlanejamentoId",
                table: "RecebimentoEvento",
                column: "ItemPlanejamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamento_Produto_ProdutoId",
                table: "Agendamento",
                column: "ProdutoId",
                principalTable: "Produto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamento_Produto_ProdutoId",
                table: "Agendamento");

            migrationBuilder.DropTable(
                name: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_Agendamento_ProdutoId",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "Agendamento");
        }
    }
}
