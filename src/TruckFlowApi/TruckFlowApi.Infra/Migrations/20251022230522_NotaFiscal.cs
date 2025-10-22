using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class NotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Numero",
                table: "NotaFiscal",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "AgendamentoId",
                table: "NotaFiscal",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveAcesso",
                table: "NotaFiscal",
                type: "nvarchar(44)",
                maxLength: 44,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataEmissao",
                table: "NotaFiscal",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DestinatarioCpfCnpj",
                table: "NotaFiscal",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinatarioNome",
                table: "NotaFiscal",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitenteCnpj",
                table: "NotaFiscal",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitenteNome",
                table: "NotaFiscal",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PesoBruto",
                table: "NotaFiscal",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoLiquido",
                table: "NotaFiscal",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacaVeiculo",
                table: "NotaFiscal",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawXml",
                table: "NotaFiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Serie",
                table: "NotaFiscal",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "NotaFiscal",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "NotaFiscal",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedByUserId",
                table: "NotaFiscal",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ValidationMessages",
                table: "NotaFiscal",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorTotal",
                table: "NotaFiscal",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VolumeQuantidade",
                table: "NotaFiscal",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotaFiscalItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItem_NotaFiscal_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotaFiscal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscal_ChaveAcesso",
                table: "NotaFiscal",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItem_NotaFiscalId",
                table: "NotaFiscalItem",
                column: "NotaFiscalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaFiscalItem");

            migrationBuilder.DropIndex(
                name: "IX_NotaFiscal_ChaveAcesso",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "AgendamentoId",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "ChaveAcesso",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "DataEmissao",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "DestinatarioCpfCnpj",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "DestinatarioNome",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "EmitenteCnpj",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "EmitenteNome",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "PesoBruto",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "PesoLiquido",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "PlacaVeiculo",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "RawXml",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValidationMessages",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorTotal",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "VolumeQuantidade",
                table: "NotaFiscal");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "NotaFiscal",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
