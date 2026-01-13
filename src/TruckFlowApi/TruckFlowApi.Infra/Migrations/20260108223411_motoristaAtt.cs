using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class motoristaAtt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Motorista",
                newName: "NomeReal");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Motorista",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Motorista");

            migrationBuilder.RenameColumn(
                name: "NomeReal",
                table: "Motorista",
                newName: "Nome");
        }
    }
}
