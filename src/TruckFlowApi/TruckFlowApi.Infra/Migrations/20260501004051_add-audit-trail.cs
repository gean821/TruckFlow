using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class addaudittrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Veiculo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Veiculo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "UnidadeEntrega",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "UnidadeEntrega",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "RecebimentoEvento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Produto",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Produto",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "PlanejamentoRecebimento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "PlanejamentoRecebimento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Notificacao",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Notificacao",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "NotaFiscalItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "NotaFiscalItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "NotaFiscal",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "NotaFiscal",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Motorista",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Motorista",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "LocalDescarga",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "LocalDescarga",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ItemPlanejamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "ItemPlanejamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Grade",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Grade",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Fornecedor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Fornecedor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Empresa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Empresa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Carga",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Carga",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Agendamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Agendamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Administrador",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Administrador",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangesJson = table.Column<string>(type: "jsonb", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EmpresaId_EntityName_EntityId",
                table: "AuditLog",
                columns: new[] { "EmpresaId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EmpresaId_Timestamp",
                table: "AuditLog",
                columns: new[] { "EmpresaId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EmpresaId_UserId",
                table: "AuditLog",
                columns: new[] { "EmpresaId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UnidadeEntrega");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UnidadeEntrega");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "RecebimentoEvento");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Produto");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PlanejamentoRecebimento");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PlanejamentoRecebimento");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Notificacao");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Notificacao");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "NotaFiscalItem");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "NotaFiscal");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Motorista");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Motorista");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "LocalDescarga");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ItemPlanejamento");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ItemPlanejamento");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Grade");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Grade");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Fornecedor");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Carga");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Carga");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Administrador");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Administrador");
        }
    }
}
