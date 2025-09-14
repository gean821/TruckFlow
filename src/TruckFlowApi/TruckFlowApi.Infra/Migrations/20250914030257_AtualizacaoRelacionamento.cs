using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AtualizacaoRelacionamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocalDescarga_Produto_ProdutoId",
                table: "LocalDescarga");

            migrationBuilder.DropIndex(
                name: "IX_LocalDescarga_ProdutoId",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "LocalDescarga");

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDescargaId",
                table: "Produto",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Produto_LocalDescargaId",
                table: "Produto",
                column: "LocalDescargaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produto_LocalDescarga_LocalDescargaId",
                table: "Produto",
                column: "LocalDescargaId",
                principalTable: "LocalDescarga",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produto_LocalDescarga_LocalDescargaId",
                table: "Produto");

            migrationBuilder.DropIndex(
                name: "IX_Produto_LocalDescargaId",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "LocalDescargaId",
                table: "Produto");

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "LocalDescarga",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LocalDescarga_ProdutoId",
                table: "LocalDescarga",
                column: "ProdutoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalDescarga_Produto_ProdutoId",
                table: "LocalDescarga",
                column: "ProdutoId",
                principalTable: "Produto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
