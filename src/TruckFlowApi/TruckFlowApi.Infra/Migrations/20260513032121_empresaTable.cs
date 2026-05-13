using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class empresaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_AgendamentoId",
                table: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_EmpresaId",
                table: "RecebimentoEvento");

            migrationBuilder.AlterColumn<Guid>(
                name: "ItemPlanejamentoId",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "FornecedorId",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "RecebimentoEvento",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantidadeReservada",
                table: "ItemPlanejamento",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_AgendamentoId_Tipo",
                table: "RecebimentoEvento",
                columns: new[] { "AgendamentoId", "Tipo" },
                unique: true,
                filter: "\"AgendamentoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_EmpresaId_ItemPlanejamentoId",
                table: "RecebimentoEvento",
                columns: new[] { "EmpresaId", "ItemPlanejamentoId" },
                filter: "\"ItemPlanejamentoId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_FornecedorId",
                table: "RecebimentoEvento",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_ProdutoId",
                table: "RecebimentoEvento",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecebimentoEvento_Fornecedor_FornecedorId",
                table: "RecebimentoEvento",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecebimentoEvento_Produto_ProdutoId",
                table: "RecebimentoEvento",
                column: "ProdutoId",
                principalTable: "Produto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecebimentoEvento_Fornecedor_FornecedorId",
                table: "RecebimentoEvento");

            migrationBuilder.DropForeignKey(
                name: "FK_RecebimentoEvento_Produto_ProdutoId",
                table: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_AgendamentoId_Tipo",
                table: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_EmpresaId_ItemPlanejamentoId",
                table: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_FornecedorId",
                table: "RecebimentoEvento");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoEvento_ProdutoId",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "FornecedorId",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "QuantidadeReservada",
                table: "ItemPlanejamento");

            migrationBuilder.AlterColumn<Guid>(
                name: "ItemPlanejamentoId",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_AgendamentoId",
                table: "RecebimentoEvento",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoEvento_EmpresaId",
                table: "RecebimentoEvento",
                column: "EmpresaId");
        }
    }
}
