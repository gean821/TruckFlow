using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;
using TruckFlowApi.Infra.Repositories;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class GradeServiceTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DeleteGrade_ComAgendamentoFinalizado_LancaBusinessExceptionENaoRemoveNada()
    {
        var empresaId = Guid.NewGuid();
        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaId);
        using (var db = Fixture.CreateDbContext(empresaContext))
        {
            var (localDescarga, produto) = await PrepararAmbienteAsync(
                db, empresaId, "Aurora Teste - Delete Grade Finalizada");

            var currentUser = NovoCurrentUser(empresaId);
            var service = ServiceFactory.CreateGradeService(db, currentUser);
            var hoje = HojeLocal();

            var grade = await service.CreateGrade(
                new GradeCreateDto
                {
                    ProdutoId = produto.Id,
                    LocalDescargaId = localDescarga.Id,
                    DataInicio = hoje,
                    DataFim = hoje.AddDays(2),
                    HoraInicial = new TimeOnly(8, 0),
                    HoraFinal = new TimeOnly(10, 0),
                    IntervaloMinutos = 60,
                    DiasSemana = "0,1,2,3,4,5,6",
                }
            );

            var agendamentos = await db.Agendamento.Where(a => a.GradeId == grade.Id).ToListAsync();
            Assert.NotEmpty(agendamentos);
            var totalOriginal = agendamentos.Count;

            agendamentos[0].StatusAgendamento = StatusAgendamento.Finalizado;
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<BusinessException>(() => service.DeleteGrade(grade.Id));

            var gradeAindaExiste = await db.Grade.AsNoTracking().AnyAsync(g => g.Id == grade.Id);
            Assert.True(gradeAindaExiste, "A grade não deveria ter sido removida.");

            var totalAposTentativa = await db
                .Agendamento.AsNoTracking()
                .CountAsync(a => a.GradeId == grade.Id);
            Assert.Equal(totalOriginal, totalAposTentativa);
        }
    }

    [Fact]
    public async Task UpdateGrade_FalhaAoInserirNovosSlots_RevertaTransacaoESlotsAntigosPermanecem()
    {
        var empresaId = Guid.NewGuid();
        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaId);
        using (var db = Fixture.CreateDbContext(empresaContext))
        {
            var (localDescarga, produto) = await PrepararAmbienteAsync(
                db, empresaId, "Aurora Teste - Rollback Update Grade");

            var currentUser = NovoCurrentUser(empresaId);
            var hoje = HojeLocal();

            var serviceDeCriacao = ServiceFactory.CreateGradeService(db, currentUser);
            var grade = await serviceDeCriacao.CreateGrade(
                new GradeCreateDto
                {
                    ProdutoId = produto.Id,
                    LocalDescargaId = localDescarga.Id,
                    DataInicio = hoje,
                    DataFim = hoje.AddDays(3),
                    HoraInicial = new TimeOnly(8, 0),
                    HoraFinal = new TimeOnly(10, 0),
                    IntervaloMinutos = 60,
                    DiasSemana = "0,1,2,3,4,5,6",
                }
            );

            var slotsAntesIds = await db
                .Agendamento.AsNoTracking()
                .Where(a => a.GradeId == grade.Id)
                .Select(a => a.Id)
                .ToListAsync();
            Assert.NotEmpty(slotsAntesIds);

            var repoComFalha = new ThrowingOnAddRangeAgendamentoRepositorio(
                new AgendamentoRepositorio(db)
            );
            var serviceComFalha = ServiceFactory.CreateGradeService(db, currentUser, repoComFalha);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serviceComFalha.UpdateGrade(grade.Id, new GradeUpdateDto { IntervaloMinutos = 30 })
            );

            var slotsDepoisIds = await db
                .Agendamento.AsNoTracking()
                .Where(a => a.GradeId == grade.Id)
                .Select(a => a.Id)
                .ToListAsync();

            Assert.Equal(slotsAntesIds.OrderBy(x => x), slotsDepoisIds.OrderBy(x => x));
        }
    }

    [Fact]
    public async Task UpdateGrade_ComDataInicioNoPassado_NaoRegeneraSlotsParaDatasPassadas()
    {
        var empresaId = Guid.NewGuid();
        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaId);
        using (var db = Fixture.CreateDbContext(empresaContext))
        {
            var (localDescarga, produto) = await PrepararAmbienteAsync(
                db, empresaId, "Aurora Teste - Grade Data Passada");

            var hoje = HojeLocal();

            var grade = TestDataSeeder.NovaGrade(
                empresaId,
                produto.Id,
                produto,
                localDescarga.Id,
                localDescarga,
                unidadeEntregaId: localDescarga.UnidadeEntregaId,
                dataInicio: hoje.AddDays(-10),
                dataFim: hoje.AddDays(10),
                horaInicial: new TimeOnly(8, 0),
                horaFinal: new TimeOnly(10, 0),
                intervaloMinutos: 120
            );

            db.Grade.Add(grade);
            await db.SaveChangesAsync();

            var currentUser = NovoCurrentUser(empresaId);
            var service = ServiceFactory.CreateGradeService(db, currentUser);

            await service.UpdateGrade(grade.Id, new GradeUpdateDto { IntervaloMinutos = 60 });

            var slotsGerados = await db
                .Agendamento.AsNoTracking()
                .Where(a => a.GradeId == grade.Id)
                .ToListAsync();

            Assert.NotEmpty(slotsGerados);
            Assert.All(
                slotsGerados,
                s =>
                {
                    var dataLocal = DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(s.DataInicio, Grade.OperationalTimeZone)
                    );
                    Assert.True(dataLocal >= hoje, $"Slot gerado para data passada: {dataLocal}");
                }
            );

            Assert.Contains(
                slotsGerados,
                s =>
                    DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(s.DataInicio, Grade.OperationalTimeZone)
                    ) == hoje.AddDays(5)
            );
        }
    }

    [Fact]
    public async Task UpdateGrade_ComConflitoNaNovaJanela_LancaBusinessExceptionENaoFiltraEmSilencio()
    {
        var empresaId = Guid.NewGuid();
        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaId);
        using (var db = Fixture.CreateDbContext(empresaContext))
        {
            var (localDescarga, produto) = await PrepararAmbienteAsync(
                db, empresaId, "Aurora Teste - Conflito Update Grade");

            var currentUser = NovoCurrentUser(empresaId);
            var service = ServiceFactory.CreateGradeService(db, currentUser);
            var hoje = HojeLocal();

            var grade = await service.CreateGrade(
                new GradeCreateDto
                {
                    ProdutoId = produto.Id,
                    LocalDescargaId = localDescarga.Id,
                    DataInicio = hoje,
                    DataFim = hoje.AddDays(5),
                    HoraInicial = new TimeOnly(14, 0),
                    HoraFinal = new TimeOnly(16, 0),
                    IntervaloMinutos = 60,
                    DiasSemana = "0,1,2,3,4,5,6",
                }
            );

            var slotsOriginaisIds = await db
                .Agendamento.AsNoTracking()
                .Where(a => a.GradeId == grade.Id)
                .Select(a => a.Id)
                .ToListAsync();

            var amanha = hoje.AddDays(1);
            var inicioUtc = ParaUtc(amanha, new TimeOnly(9, 0));
            var fimUtc = ParaUtc(amanha, new TimeOnly(10, 0));

            db.Agendamento.Add(
                TestDataSeeder.NovoAgendamentoAtivoNaDoca(
                    empresaId,
                    localDescarga.Id,
                    localDescarga.UnidadeEntregaId,
                    produto.Id,
                    inicioUtc,
                    fimUtc
                )
            );
            await db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                service.UpdateGrade(
                    grade.Id,
                    new GradeUpdateDto
                    {
                        HoraInicial = new TimeOnly(8, 0),
                        HoraFinal = new TimeOnly(10, 0),
                    }
                )
            );

            Assert.Contains("Conflito", ex.Message);

            var slotsDepoisIds = await db
                .Agendamento.AsNoTracking()
                .Where(a => a.GradeId == grade.Id)
                .Select(a => a.Id)
                .ToListAsync();

            Assert.Equal(slotsOriginaisIds.OrderBy(x => x), slotsDepoisIds.OrderBy(x => x));
        }
    }

    // Recebe db/empresaId já prontos (WithTenant precisa estar ativo no método de teste que
    // chama este helper — ver nota em cada [Fact]: a troca de AsyncLocal feita dentro de um
    // método async aninhado não é garantida de "voltar" visível pro chamador depois do await).
    private static async Task<(LocalDescarga LocalDescarga, Produto Produto)> PrepararAmbienteAsync(
        TruckFlowApi.Infra.Database.AppDbContext db,
        Guid empresaId,
        string nomeEmpresa)
    {
        var empresaSeed = TestDataSeeder.NovaEmpresa(nomeEmpresa);
        empresaSeed.Id = empresaId;

        db.Empresa.Add(empresaSeed);
        await db.SaveChangesAsync();

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        db.UnidadeEntrega.Add(unidade);
        await db.SaveChangesAsync();

        var localDescarga = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        db.LocalDescarga.Add(localDescarga);
        await db.SaveChangesAsync();

        var produto = TestDataSeeder.NovoProduto(empresaId, localDescarga.Id, localDescarga);
        db.Produto.Add(produto);
        await db.SaveChangesAsync();

        return (localDescarga, produto);
    }

    private static FakeCurrentUserService NovoCurrentUser(Guid empresaId) =>
        new()
        {
            UserId = Guid.NewGuid(),
            EmpresaId = empresaId,
            IsAdmin = true,
            IsMotorista = false,
        };

    private static DateOnly HojeLocal() =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone)
        );

    private static DateTime ParaUtc(DateOnly data, TimeOnly hora) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(hora), DateTimeKind.Unspecified),
            Grade.OperationalTimeZone
        );
}
