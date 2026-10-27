using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class NotificacaoMotoristaEventosTests : IntegrationTestBase
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 20, 13, 0, 0, TimeSpan.Zero);

    public NotificacaoMotoristaEventosTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task JanelaProxima_AvisaUmaVezSoQuemTemReservaDentroDaAntecedencia()
    {
        var cenario = await CriarCenarioAsync();

        var dentro = await AdicionarAsync(cenario, StatusAgendamento.Agendado, Agora.UtcDateTime.AddMinutes(40), cenario.MotoristaId);
        await AdicionarAsync(cenario, StatusAgendamento.Agendado, Agora.UtcDateTime.AddHours(2), cenario.MotoristaId);
        await AdicionarAsync(cenario, StatusAgendamento.Agendado, Agora.UtcDateTime.AddMinutes(-5), cenario.MotoristaId);
        await AdicionarAsync(cenario, StatusAgendamento.Disponivel, Agora.UtcDateTime.AddMinutes(20), null);
        await AdicionarAsync(cenario, StatusAgendamento.Cancelado, Agora.UtcDateTime.AddMinutes(55), cenario.MotoristaId);

        Assert.Equal(1, await EnviarAsync(cenario));
        Assert.Equal(0, await EnviarAsync(cenario));

        await using var db = Fixture.CreateDbContext();
        var notificacao = await db.Notificacao.IgnoreQueryFilters().Include(n => n.Entregas)
            .SingleAsync(n => n.EmpresaId == cenario.EmpresaId && n.Tipo == TipoNotificacao.JanelaPropxima);

        Assert.Equal(cenario.MotoristaId, notificacao.DestinatarioUsuarioId);
        Assert.Equal("Sua janela começa em 40 minutos", notificacao.Titulo);
        Assert.Contains("Descarga às 10:40 em Doca 1 • Unidade Teste", notificacao.Corpo);
        Assert.Contains(dentro.ToString(), notificacao.Payload);
        Assert.Contains(notificacao.Entregas, e => e.Canal == CanalNotificacao.Push && e.Status == StatusEntregaNotificacao.Pendente);
    }

    [Fact]
    public async Task JanelaProxima_DepoisDeReagendar_AvisaDeNovo()
    {
        var cenario = await CriarCenarioAsync();
        var id = await AdicionarAsync(cenario, StatusAgendamento.Agendado, Agora.UtcDateTime.AddMinutes(30), cenario.MotoristaId);

        Assert.Equal(1, await EnviarAsync(cenario));

        using (cenario.EmpresaContext.WithTenant(cenario.EmpresaId))
        await using (var db = Fixture.CreateDbContext(cenario.EmpresaContext))
        {
            var agendamento = await db.Agendamento.SingleAsync(a => a.Id == id);
            agendamento.Reagendar(Agora.UtcDateTime.AddMinutes(50), Agora.UtcDateTime.AddMinutes(60));
            await db.SaveChangesAsync();
        }

        Assert.Equal(1, await EnviarAsync(cenario));
    }

    [Fact]
    public async Task ConfirmadoEExpirado_GravamAvisoComHorarioDeBrasilia()
    {
        var cenario = await CriarCenarioAsync();
        var inicio = new DateTime(2026, 10, 21, 14, 30, 0, DateTimeKind.Utc);
        var id = await AdicionarAsync(cenario, StatusAgendamento.Agendado, inicio, cenario.MotoristaId);

        using (cenario.EmpresaContext.WithTenant(cenario.EmpresaId))
        await using (var db = Fixture.CreateDbContext(cenario.EmpresaContext))
        {
            await new AgendamentoConfirmadoNotificacaoHandler(db).HandleAsync(
                new AgendamentoEvent.AgendamentoConfirmadoEvent(id, cenario.EmpresaId, cenario.MotoristaId, inicio, DateTime.UtcNow),
                CancellationToken.None);
            await new AgendamentoExpiradoNotificacaoHandler(db).HandleAsync(
                new AgendamentoEvent.AgendamentoExpiradoEvent(id, cenario.EmpresaId, cenario.MotoristaId, inicio, DateTime.UtcNow),
                CancellationToken.None);
            await db.SaveChangesAsync();
        }

        await using var leitura = Fixture.CreateDbContext();
        var avisos = await leitura.Notificacao.IgnoreQueryFilters()
            .Where(n => n.EmpresaId == cenario.EmpresaId)
            .ToListAsync();

        var confirmado = Assert.Single(avisos, n => n.Tipo == TipoNotificacao.AgendamentoConfirmado);
        Assert.Equal("Descarga marcada para 21/10 às 11:30 em Doca 1 • Unidade Teste • " + cenario.EmpresaNome + ".", confirmado.Corpo);

        var expirado = Assert.Single(avisos, n => n.Tipo == TipoNotificacao.AgendamentoExpirado);
        Assert.StartsWith("O horário de 21/10 às 11:30 passou sem check-in.", expirado.Corpo);
    }

    [Theory]
    [InlineData(60, "Sua janela começa em 1 hora")]
    [InlineData(56, "Sua janela começa em 1 hora")]
    [InlineData(15, "Sua janela começa em 15 minutos")]
    [InlineData(1, "Sua janela começa agora")]
    public void Titulo_FicaLegivelParaOMotorista(int minutos, string esperado)
    {
        Assert.Equal(esperado, JanelaProximaNotificacaoService.Titulo(TimeSpan.FromMinutes(minutos)));
    }

    private sealed record Cenario(
        Guid EmpresaId,
        string EmpresaNome,
        Guid UnidadeId,
        Guid DocaId,
        Guid ProdutoId,
        Guid MotoristaId,
        TruckFlow.Domain.Contracts.IEmpresaContext EmpresaContext);

    private async Task<Cenario> CriarCenarioAsync()
    {
        var empresa = TestDataSeeder.NovaEmpresa($"Empresa Aviso {Guid.NewGuid():N}"[..30]);
        var motorista = TestDataSeeder.NovoUsuarioMotorista(empresa.Id);
        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresa.Id);
        var doca = TestDataSeeder.NovoLocalDescarga(empresa.Id, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresa.Id, doca.Id, doca);
        var empresaContext = ServiceFactory.NewEmpresaContext();

        await using (var db = Fixture.CreateDbContext())
        {
            db.Empresa.Add(empresa);
            await db.SaveChangesAsync();
            db.Usuario.Add(motorista);
            await db.SaveChangesAsync();
        }

        using (empresaContext.WithTenant(empresa.Id))
        await using (var db = Fixture.CreateDbContext(empresaContext))
        {
            db.UnidadeEntrega.Add(unidade);
            db.LocalDescarga.Add(doca);
            db.Produto.Add(produto);
            await db.SaveChangesAsync();
        }

        return new Cenario(empresa.Id, empresa.NomeFantasia, unidade.Id, doca.Id, produto.Id, motorista.Id, empresaContext);
    }

    private async Task<Guid> AdicionarAsync(Cenario cenario, StatusAgendamento status, DateTime inicio, Guid? usuarioId)
    {
        var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
            cenario.EmpresaId, cenario.DocaId, cenario.UnidadeId, cenario.ProdutoId, inicio, inicio.AddMinutes(10), status);
        agendamento.UsuarioId = usuarioId;

        using (cenario.EmpresaContext.WithTenant(cenario.EmpresaId))
        await using (var db = Fixture.CreateDbContext(cenario.EmpresaContext))
        {
            db.Agendamento.Add(agendamento);
            await db.SaveChangesAsync();
        }

        return agendamento.Id;
    }

    private async Task<int> EnviarAsync(Cenario cenario)
    {
        using var tenant = cenario.EmpresaContext.WithTenant(cenario.EmpresaId);
        await using var db = Fixture.CreateDbContext(cenario.EmpresaContext);

        var service = new JanelaProximaNotificacaoService(
            db,
            new FakeTimeProvider(Agora),
            Options.Create(new JanelaProximaOptions()),
            NullLogger<JanelaProximaNotificacaoService>.Instance);

        return await service.EnviarLembretesAsync(cenario.EmpresaId);
    }
}
