using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class makefornecedoropcionallinkprodutonoitem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Fornecedor_FornecedorId",
                table: "Grade");

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "NotaFiscalItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FornecedorId",
                table: "Grade",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "FornecedorId",
                table: "Agendamento",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItem_ProdutoId",
                table: "NotaFiscalItem",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Fornecedor_FornecedorId",
                table: "Grade",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotaFiscalItem_Produto_ProdutoId",
                table: "NotaFiscalItem",
                column: "ProdutoId",
                principalTable: "Produto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Fornecedor_FornecedorId",
                table: "Grade");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaFiscalItem_Produto_ProdutoId",
                table: "NotaFiscalItem");

            migrationBuilder.DropIndex(
                name: "IX_NotaFiscalItem_ProdutoId",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "NotaFiscalItem");

            migrationBuilder.AlterColumn<Guid>(
                name: "FornecedorId",
                table: "Grade",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FornecedorId",
                table: "Agendamento",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Fornecedor_FornecedorId",
                table: "Grade",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
