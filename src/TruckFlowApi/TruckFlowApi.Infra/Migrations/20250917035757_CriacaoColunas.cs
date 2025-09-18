using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoColunas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Veiculo",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Veiculo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Veiculo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UnidadeEntrega",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UnidadeEntrega",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UnidadeEntrega",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Produto",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Produto",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Produto",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Notificacao",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Notificacao",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Notificacao",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LocalDescarga",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LocalDescarga",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LocalDescarga",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Fornecedor",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Fornecedor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                table: "Fornecedor",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Fornecedor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Carga",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Carga",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Carga",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedor_ProdutoId",
                table: "Fornecedor",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedor_Produto_ProdutoId",
                table: "Fornecedor",
                column: "ProdutoId",
                principalTable: "Produto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedor_Produto_ProdutoId",
                table: "Fornecedor");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedor_ProdutoId",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UnidadeEntrega");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UnidadeEntrega");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UnidadeEntrega");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Notificacao");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Notificacao");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Notificacao");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Carga");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Carga");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Carga");
        }
    }
}
