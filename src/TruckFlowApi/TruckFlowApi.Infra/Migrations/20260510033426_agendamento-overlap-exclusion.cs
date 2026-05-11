using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckFlowApi.Infra.Migrations
{
    /// <inheritdoc />
    public partial class agendamentooverlapexclusion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Necessário para combinar `=` (igualdade de Guid) com `&&` (overlap de range) num índice GiST.
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // Constraint de exclusão: na MESMA doca, dois agendamentos com janelas [DataInicio, DataFim)
            // sobrepostas não podem coexistir se ambos estiverem em estado ATIVO.
            // Estados ativos = 0 Disponivel | 1 Pendente | 2 Agendado | 3 EmAndamento.
            // Cancelado (5), Expirado (6) e Finalizado (4) ficam fora — não bloqueiam.
            // LocalDescargaId NULL (legado pré-migration) também fica fora.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Agendamento""
                ADD CONSTRAINT ""agendamento_no_overlap_per_doca""
                EXCLUDE USING gist (
                    ""LocalDescargaId"" WITH =,
                    tstzrange(""DataInicio"", ""DataFim"", '[)') WITH &&
                )
                WHERE (
                    ""LocalDescargaId"" IS NOT NULL
                    AND ""StatusAgendamento"" IN (0, 1, 2, 3)
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Agendamento""
                DROP CONSTRAINT IF EXISTS ""agendamento_no_overlap_per_doca"";
            ");
            // Não derrubo a extensão btree_gist no Down — outras coisas podem depender dela.
        }
    }
}
