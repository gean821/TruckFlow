using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class NotificacaoListagemFiltrosTests : IntegrationTestBase
{
    public NotificacaoListagemFiltrosTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ListarMinhasAsync_ComSearch_RetornaSoNotificacoesQueBatemNoTituloOuCorpo()
    {
        var empresa = TestDataSeeder.NovaEmpresa("Empresa Filtro Notificacao");
        var admin = TestDataSeeder.NovoUsuarioMotorista(empresa.Id);

        var empresaContext = ServiceFactory.NewEmpresaContext();

        using (var setupDb = Fixture.CreateDbContext())
        {
            setupDb.Empresa.Add(empresa);
            await setupDb.SaveChangesAsync();
            setupDb.Usuario.Add(admin);
            await setupDb.SaveChangesAsync();
        }

        using (var tenantSetupScope = empresaContext.WithTenant(empresa.Id))
        using (var setupDb = Fixture.CreateDbContext(empresaContext))
        {
            setupDb.Notificacao.AddRange(
                NovaNotificacao(empresa.Id, admin.Id, "Motorista chegou", "Chegada registrada"),
                NovaNotificacao(empresa.Id, admin.Id, "Agendamento cancelado", "Cancelado pelo motorista"),
                NovaNotificacao(empresa.Id, admin.Id, "Nova mensagem", "Motorista pediu doca 2"));
            await setupDb.SaveChangesAsync();
        }

        using var tenantScope = empresaContext.WithTenant(empresa.Id);
        using var db = Fixture.CreateDbContext(empresaContext);

        var currentUser = new FakeCurrentUserService
        {
            UserId = admin.Id,
            EmpresaId = empresa.Id,
            IsMotorista = false,
        };

        var service = new NotificacaoService(new NotificacaoRepositorio(db), currentUser, empresaContext);

        var porTitulo = await service.ListarMinhasAsync(
            new NotificacaoListQueryDto { Search = "chegou" }, CancellationToken.None);
        Assert.Single(porTitulo.Items);
        Assert.Equal("Motorista chegou", porTitulo.Items.First().Titulo);

        var porCorpo = await service.ListarMinhasAsync(
            new NotificacaoListQueryDto { Search = "doca" }, CancellationToken.None);
        Assert.Single(porCorpo.Items);
        Assert.Equal("Nova mensagem", porCorpo.Items.First().Titulo);

        var semMatch = await service.ListarMinhasAsync(
            new NotificacaoListQueryDto { Search = "inexistente-xyz" }, CancellationToken.None);
        Assert.Empty(semMatch.Items);
    }

    [Fact]
    public async Task ListarMinhasAsync_ComPeriodo_RetornaSoNotificacoesDentroDoIntervalo()
    {
        var empresa = TestDataSeeder.NovaEmpresa("Empresa Periodo Notificacao");
        var admin = TestDataSeeder.NovoUsuarioMotorista(empresa.Id);

        var antiga = NovaNotificacao(empresa.Id, admin.Id, "Antiga", "Fora do periodo");
        antiga.CreatedAt = DateTime.UtcNow.AddDays(-30);

        var dentro = NovaNotificacao(empresa.Id, admin.Id, "Dentro do periodo", "Deve aparecer");
        dentro.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var empresaContext = ServiceFactory.NewEmpresaContext();

        using (var setupDb = Fixture.CreateDbContext())
        {
            setupDb.Empresa.Add(empresa);
            await setupDb.SaveChangesAsync();
            setupDb.Usuario.Add(admin);
            await setupDb.SaveChangesAsync();
        }

        using (var tenantSetupScope = empresaContext.WithTenant(empresa.Id))
        using (var setupDb = Fixture.CreateDbContext(empresaContext))
        {
            setupDb.Notificacao.AddRange(antiga, dentro);
            await setupDb.SaveChangesAsync();
        }

        using var tenantScope = empresaContext.WithTenant(empresa.Id);
        using var db = Fixture.CreateDbContext(empresaContext);

        var currentUser = new FakeCurrentUserService
        {
            UserId = admin.Id,
            EmpresaId = empresa.Id,
            IsMotorista = false,
        };

        var service = new NotificacaoService(new NotificacaoRepositorio(db), currentUser, empresaContext);

        var resultado = await service.ListarMinhasAsync(
            new NotificacaoListQueryDto { DataInicio = DateTime.UtcNow.AddDays(-7) },
            CancellationToken.None);

        Assert.Single(resultado.Items);
        Assert.Equal("Dentro do periodo", resultado.Items.First().Titulo);
    }

    private static Notificacao NovaNotificacao(
        Guid empresaId, Guid destinatarioUsuarioId, string titulo, string corpo) =>
        new()
        {
            EmpresaId = empresaId,
            DestinatarioUsuarioId = destinatarioUsuarioId,
            Tipo = TipoNotificacao.MotoristaChegou,
            Prioridade = PrioridadeNotificacao.Normal,
            Titulo = titulo,
            Corpo = corpo,
            Payload = "{}",
            CreatedAt = DateTime.UtcNow,
        };
}
