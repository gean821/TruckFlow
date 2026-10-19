using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class NotificacaoFixesTests : IntegrationTestBase
{
    public NotificacaoFixesTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task EnviarParaEmpresaAsync_ComFanOutParaMultiplosAdmins_MotoristaVeApenasUmaMensagemNaConversa()
    {
        var empresaA = TestDataSeeder.NovaEmpresa("Empresa A");
        var empresaAId = empresaA.Id;

        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaAId);
        using var db = Fixture.CreateDbContext(empresaContext);

        db.Empresa.Add(empresaA);
        await db.SaveChangesAsync();

        var unidadeA = TestDataSeeder.NovaUnidadeEntrega(empresaAId);
        db.UnidadeEntrega.Add(unidadeA);
        await db.SaveChangesAsync();

        var roleAdmin = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = Roles.Admin,
            NormalizedName = Roles.Admin.ToUpperInvariant(),
        };
        db.Roles.Add(roleAdmin);

        var admin1 = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        var admin2 = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        var motorista = TestDataSeeder.NovoUsuarioMotorista();
        db.Usuario.AddRange(admin1, admin2, motorista);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = admin1.Id, RoleId = roleAdmin.Id });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = admin2.Id, RoleId = roleAdmin.Id });
        await db.SaveChangesAsync();

        var agendamento = TestDataSeeder.NovaVagaDisponivel(empresaAId, unidadeA.Id, Guid.NewGuid());
        agendamento.ProdutoId = null;
        agendamento.StatusAgendamento = StatusAgendamento.Agendado;
        agendamento.UsuarioId = motorista.Id;
        db.Agendamento.Add(agendamento);
        await db.SaveChangesAsync();

        await using var identityProvider = CriarUserManagerProvider(db);
        var userManager = identityProvider.GetRequiredService<UserManager<Usuario>>();

        var notificacaoRepo = new NotificacaoRepositorio(db);
        var agendamentoRepo = new AgendamentoRepositorio(db);

        var currentUserMotorista = new FakeCurrentUserService
        {
            UserId = motorista.Id,
            EmpresaId = null,
            IsMotorista = true,
        };

        var sendService = new NotificacaoSendService(
            notificacaoRepo,
            agendamentoRepo,
            userManager,
            currentUserMotorista,
            empresaContext,
            NullLogger<NotificacaoSendService>.Instance);

        await sendService.EnviarParaEmpresaAsync(
            new EnviarParaEmpresaDto
            {
                AgendamentoId = agendamento.Id,
                Titulo = "Aviso",
                Corpo = "Estou chegando em 10 minutos.",
            },
            CancellationToken.None);

        // Confirma que o fan-out de fato criou 1 cópia por admin (pré-condição do cenário).
        var totalNotificacoesCriadas = await db.Notificacao
            .IgnoreQueryFilters()
            .CountAsync(n => n.Tipo == TipoNotificacao.MensagemManualMotorista);
        Assert.Equal(2, totalNotificacoesCriadas);

        var conversaDoMotorista = await notificacaoRepo.ListByUserAndAgendamentoAcrossTenantsAsync(
            motorista.Id, agendamento.Id);

        Assert.Single(conversaDoMotorista);
    }

    [Fact]
    public async Task MotoristaChegouNotificacaoHandler_EnriquecePayloadComContextoDoAgendamento()
    {
        var empresaA = TestDataSeeder.NovaEmpresa("Empresa A");
        var empresaAId = empresaA.Id;

        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaAId);
        using var db = Fixture.CreateDbContext(empresaContext);

        db.Empresa.Add(empresaA);
        await db.SaveChangesAsync();

        var unidadeA = TestDataSeeder.NovaUnidadeEntrega(empresaAId);
        db.UnidadeEntrega.Add(unidadeA);
        await db.SaveChangesAsync();

        var localDescargaA = TestDataSeeder.NovoLocalDescarga(empresaAId, unidadeA.Id, unidadeA);
        db.LocalDescarga.Add(localDescargaA);
        await db.SaveChangesAsync();

        var roleAdmin = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = Roles.Admin,
            NormalizedName = Roles.Admin.ToUpperInvariant(),
        };
        db.Roles.Add(roleAdmin);

        var admin = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        var motorista = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        db.Usuario.AddRange(admin, motorista);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = admin.Id, RoleId = roleAdmin.Id });
        await db.SaveChangesAsync();

        var dataInicio = DateTime.UtcNow.AddHours(1);
        var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
            empresaAId,
            localDescargaA.Id,
            unidadeA.Id,
            Guid.NewGuid(),
            dataInicio,
            dataInicio.AddHours(2),
            StatusAgendamento.EmAndamento);
        agendamento.ProdutoId = null;
        agendamento.UsuarioId = motorista.Id;
        db.Agendamento.Add(agendamento);
        await db.SaveChangesAsync();

        await using var identityProvider = CriarUserManagerProvider(db);
        var userManager = identityProvider.GetRequiredService<UserManager<Usuario>>();

        var handler = new MotoristaChegouNotificacaoHandler(db, userManager);

        var domainEvent = new AgendamentoEvent.MotoristaChegouEvent(
            AgendamentoId: agendamento.Id,
            EmpresaId: empresaAId,
            MotoristaUsuarioId: motorista.Id,
            OcorridoEm: DateTime.UtcNow);

        await handler.HandleAsync(domainEvent, CancellationToken.None);
        await db.SaveChangesAsync();

        var notificacao = await db.Notificacao
            .AsNoTracking()
            .SingleAsync(n => n.Tipo == TipoNotificacao.MotoristaChegou && n.DestinatarioUsuarioId == admin.Id);

        using var payload = JsonDocument.Parse(notificacao.Payload);
        var root = payload.RootElement;

        Assert.Equal(unidadeA.Nome, root.GetProperty("unidadeEntregaNome").GetString());
        Assert.Equal(localDescargaA.Nome, root.GetProperty("localDescargaNome").GetString());

        var dataAgendamentoIso = root.GetProperty("dataAgendamentoIso").GetDateTime();
        Assert.Equal(agendamento.DataInicio, dataAgendamentoIso, TimeSpan.FromSeconds(1));

        Assert.Equal(agendamento.Id, root.GetProperty("agendamentoId").GetGuid());
        Assert.Equal(motorista.Id, root.GetProperty("motoristaUsuarioId").GetGuid());
    }

    [Fact]
    public async Task MarcarTodasComoLidasAsync_MarcaSomenteNotificacoesDoUsuarioLogado()
    {
        var empresaA = TestDataSeeder.NovaEmpresa("Empresa A");
        var empresaAId = empresaA.Id;

        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaAId);
        using var db = Fixture.CreateDbContext(empresaContext);

        db.Empresa.Add(empresaA);
        await db.SaveChangesAsync();

        var usuarioA = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        var usuarioB = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
        db.Usuario.AddRange(usuarioA, usuarioB);
        await db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            db.Notificacao.Add(NovaNotificacaoNaoLida(empresaAId, usuarioA.Id));
        }

        var notificacaoDeB = NovaNotificacaoNaoLida(empresaAId, usuarioB.Id);
        db.Notificacao.Add(notificacaoDeB);

        await db.SaveChangesAsync();

        var notificacaoRepo = new NotificacaoRepositorio(db);
        var currentUserA = new FakeCurrentUserService
        {
            UserId = usuarioA.Id,
            EmpresaId = empresaAId,
            IsMotorista = false,
        };

        var service = new NotificacaoService(notificacaoRepo, currentUserA, empresaContext);

        var totalMarcadas = await service.MarcarTodasComoLidasAsync(CancellationToken.None);

        Assert.Equal(3, totalMarcadas);

        var notificacoesDeA = await db.Notificacao
            .AsNoTracking()
            .Where(n => n.DestinatarioUsuarioId == usuarioA.Id)
            .ToListAsync();
        Assert.All(notificacoesDeA, n => Assert.NotNull(n.LidaEm));

        var notificacaoDeBAtualizada = await db.Notificacao
            .AsNoTracking()
            .SingleAsync(n => n.Id == notificacaoDeB.Id);
        Assert.Null(notificacaoDeBAtualizada.LidaEm);
    }

    private static Notificacao NovaNotificacaoNaoLida(Guid empresaId, Guid destinatarioUsuarioId) => new()
    {
        EmpresaId = empresaId,
        DestinatarioUsuarioId = destinatarioUsuarioId,
        Tipo = TipoNotificacao.MensagemManualAdmin,
        Prioridade = PrioridadeNotificacao.Normal,
        Titulo = "Teste",
        Corpo = "Corpo de teste",
        Payload = "{}",
        CreatedAt = DateTime.UtcNow,
    };

    private static ServiceProvider CriarUserManagerProvider(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddIdentityCore<Usuario>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services.BuildServiceProvider();
    }
}
