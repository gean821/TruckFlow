using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using static TruckFlow.Test.Relatorios.RelatorioTestData;

namespace TruckFlow.Test.Integracao
{
    /// <summary>
    /// Sobe um Postgres descartável no Docker (uma vez por classe de teste), aplica as migrations
    /// reais do projeto e insere um cenário conhecido de agendamentos.
    /// </summary>
    public sealed class PostgresRelatorioFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        public Guid EmpresaA { get; } = Guid.NewGuid();
        public Guid EmpresaB { get; } = Guid.NewGuid();

        // Cenário da Empresa A (horários em São Paulo):
        //   R1  01/10 07:30  ABC1D23  com doca "Doca 1"
        //   R2  01/10 13:00  XYZ9K88
        //   R3  01/10 22:30  AB_1234  (em UTC já é 02/10 01:30)
        //   R4  30/09 23:30  ABX1234  (em UTC já é 01/10 02:30 -> fora do período 01/10 local)
        // Empresa B:
        //   R5  01/10 10:00  ABC1D23
        public static readonly DateTime R1Inicio = LocalParaUtc(2026, 10, 1, 7, 30);
        public static readonly DateTime R2Inicio = LocalParaUtc(2026, 10, 1, 13, 0);
        public static readonly DateTime R3Inicio = LocalParaUtc(2026, 10, 1, 22, 30);
        public static readonly DateTime R4Inicio = LocalParaUtc(2026, 9, 30, 23, 30);
        public static readonly DateTime R5Inicio = LocalParaUtc(2026, 10, 1, 10, 0);

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            await using var db = CriarContexto(EmpresaA);
            await db.Database.MigrateAsync();
            await PopularAsync(db);
        }

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        public AppDbContext CriarContexto(Guid empresaId)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

            return new AppDbContext(options, new EmpresaContextFixo(empresaId));
        }

        private async Task PopularAsync(AppDbContext db)
        {
            var unidadeA = NovaUnidade(EmpresaA);
            var unidadeB = NovaUnidade(EmpresaB);
            var doca = new LocalDescarga
            {
                Nome = "Doca 1",
                UnidadeEntrega = unidadeA,
                EmpresaId = EmpresaA,
                CreatedAt = DateTime.UtcNow
            };

            db.AddRange(NovaEmpresa(EmpresaA, "11111111000111"), NovaEmpresa(EmpresaB, "22222222000122"));
            db.AddRange(unidadeA, unidadeB, doca);
            db.AddRange(
                NovoAgendamento(EmpresaA, unidadeA, R1Inicio, "ABC1D23", doca, volume: 1500m),
                NovoAgendamento(EmpresaA, unidadeA, R2Inicio, "XYZ9K88"),
                NovoAgendamento(EmpresaA, unidadeA, R3Inicio, "AB_1234"),
                NovoAgendamento(EmpresaA, unidadeA, R4Inicio, "ABX1234"),
                NovoAgendamento(EmpresaB, unidadeB, R5Inicio, "ABC1D23"));

            await db.SaveChangesAsync();
        }

        private static Empresa NovaEmpresa(Guid id, string cnpj) => new()
        {
            Id = id,
            RazaoSocial = $"Empresa {cnpj}",
            NomeFantasia = $"Empresa {cnpj}",
            Cnpj = cnpj,
            Email = $"{cnpj}@teste.com",
            Logradouro = "Rua Teste",
            Numero = "1",
            Bairro = "Centro",
            Cidade = "Chapecó",
            Estado = "SC",
            Cep = "89800000",
            Ativa = true,
            CreatedAt = DateTime.UtcNow
        };

        private static UnidadeEntrega NovaUnidade(Guid empresaId) => new()
        {
            Nome = "Unidade Teste",
            Localizacao = "Chapecó/SC",
            EmpresaId = empresaId,
            CreatedAt = DateTime.UtcNow
        };

        private static Agendamento NovoAgendamento(
            Guid empresaId,
            UnidadeEntrega unidade,
            DateTime inicioUtc,
            string placa,
            LocalDescarga? doca = null,
            decimal? volume = null) => new()
        {
            EmpresaId = empresaId,
            UnidadeEntrega = unidade,
            LocalDescarga = doca,
            TipoCarga = TipoCarga.Milho,
            StatusAgendamento = StatusAgendamento.Agendado,
            DataInicio = inicioUtc,
            DataFim = inicioUtc.AddMinutes(30),
            PlacaVeiculo = placa,
            VolumeCarga = volume,
            CreatedAt = DateTime.UtcNow
        };

        private sealed class EmpresaContextFixo(Guid empresaId) : IEmpresaContext
        {
            public Guid EmpresaId => empresaId;
            public Guid? EmpresaIdOrNull => empresaId;
            public IDisposable WithTenant(Guid id) => throw new NotSupportedException();
        }
    }
}
