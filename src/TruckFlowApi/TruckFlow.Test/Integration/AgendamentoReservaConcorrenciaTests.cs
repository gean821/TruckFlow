using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Exceptions;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class AgendamentoReservaConcorrenciaTests(DatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{

    [Fact]
    public async Task Reservar_Com100ThreadsConcorrentesNaMesmaVaga_ProduzExatamente1SucessoE99Falhas()
    {
        const int totalThreads = 100;

        Guid empresaId, vagaId;
        string chaveAcesso;
        var motoristaIds = new List<Guid>();

        var empresaSeed = TestDataSeeder.NovaEmpresa("Aurora Teste");
        empresaId = empresaSeed.Id;

        var setupEmpresaContext = ServiceFactory.NewEmpresaContext();
        using var setupTenantScope = setupEmpresaContext.WithTenant(empresaId);

        using (var setupDb = Fixture.CreateDbContext(setupEmpresaContext))
        {
            setupDb.Empresa.Add(empresaSeed);
            await setupDb.SaveChangesAsync();

            var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
            setupDb.UnidadeEntrega.Add(unidade);

            var fornecedor = TestDataSeeder.NovoFornecedor(empresaId);
            setupDb.Fornecedor.Add(fornecedor);
            await setupDb.SaveChangesAsync();

            var localDescarga = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
            setupDb.LocalDescarga.Add(localDescarga);
            await setupDb.SaveChangesAsync();

            var produto = TestDataSeeder.NovoProduto(empresaId, localDescarga.Id, localDescarga);
            setupDb.Produto.Add(produto);
            await setupDb.SaveChangesAsync();

            var nota = TestDataSeeder.NovaNotaFiscal(empresaId, fornecedor.Id, produto.Id);
            setupDb.NotaFiscal.Add(nota);
            chaveAcesso = nota.ChaveAcesso;

            var vaga = TestDataSeeder.NovaVagaDisponivel(empresaId, unidade.Id, produto.Id);
            setupDb.Agendamento.Add(vaga);

            for (var i = 0; i < totalThreads; i++)
            {
                var motorista = TestDataSeeder.NovoUsuarioMotorista(empresaId);
                setupDb.Usuario.Add(motorista);
                motoristaIds.Add(motorista.Id);
            }

            await setupDb.SaveChangesAsync();
            vagaId = vaga.Id;
        }

        var tentativas = motoristaIds.Select(motoristaId => Task.Run(async () =>
        {
            var empresaContext = ServiceFactory.NewEmpresaContext();
            using var tenantScope = empresaContext.WithTenant(empresaId);
            using var db = Fixture.CreateDbContext(empresaContext);

            var currentUser = new FakeCurrentUserService
            {
                UserId = motoristaId,
                EmpresaId = empresaId,
                IsMotorista = true,
            };

            var svc = ServiceFactory.CreateAgendamentoMotoristaService(db, empresaContext, currentUser);

            Exception? erro = null;
            var sucesso = true;

            try
            {
                await svc.BookAppointment(new ReservarAgendamentoDto
                {
                    AgendamentoId = vagaId,
                    NotaFiscalChaveAcesso = chaveAcesso,
                });
            }
            catch (Exception ex) when (ex is BusinessException or DomainException or DbUpdateConcurrencyException)
            {
                sucesso = false;
                erro = ex;
            }

            return (Sucesso: sucesso, MotoristaId: motoristaId, Erro: erro);
        })).ToArray();

        var resultados = await Task.WhenAll(tentativas);

        var falhas = resultados.Where(r => !r.Sucesso).ToList();
        var sucesso = Assert.Single(resultados, r => r.Sucesso);

        Assert.Equal(totalThreads - 1, falhas.Count);
        Assert.All(falhas, f => Assert.True(
            f.Erro!.Message.Contains("reservada", StringComparison.OrdinalIgnoreCase) ||
            f.Erro!.Message.Contains("disponível", StringComparison.OrdinalIgnoreCase),
            $"Falha inesperada (não é rejeição de concorrência conhecida): {f.Erro!.Message}"));

        using var assertDb = Fixture.CreateDbContext();
        var vagaFinal = await assertDb.Agendamento.IgnoreQueryFilters()
            .SingleAsync(a => a.Id == vagaId);

        Assert.Equal(StatusAgendamento.Agendado, vagaFinal.StatusAgendamento);
        Assert.Equal(sucesso.MotoristaId, vagaFinal.UsuarioId);
    }
}
