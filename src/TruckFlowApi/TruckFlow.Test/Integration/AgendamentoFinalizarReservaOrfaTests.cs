using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class AgendamentoFinalizarReservaOrfaTests(DatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task FinalizarAgendamento_ComReservaOrfaSemPlanejamentoAtivo_FinalizaERegistraConfirmacaoOrfa()
    {
        // Regressão: agendamento reservado sem PlanejamentoRecebimento/ItemPlanejamento ativo
        // cria uma reserva órfã (RecebimentoEvento.Tipo = Reserva, ItemPlanejamentoId = null).
        // Ao finalizar, sem planejamento ativo ainda, o fluxo precisa remover a reserva órfã
        // antes de criar a confirmação órfã (Tipo = Confirmacao) — senão colide com o índice
        // único IX_RecebimentoEvento_AgendamentoId_Tipo (23505) e o catch genérico do service
        // interpreta erroneamente como "agendamento já finalizado".
        var empresaId = Guid.NewGuid();

        var empresaSeed = TestDataSeeder.NovaEmpresa("Aurora Teste");
        empresaSeed.Id = empresaId;

        var empresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = empresaContext.WithTenant(empresaId);

        using var db = Fixture.CreateDbContext(empresaContext);

        db.Empresa.Add(empresaSeed);
        await db.SaveChangesAsync();

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        db.UnidadeEntrega.Add(unidade);

        var fornecedor = TestDataSeeder.NovoFornecedor(empresaId);
        db.Fornecedor.Add(fornecedor);
        await db.SaveChangesAsync();

        var localDescarga = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        db.LocalDescarga.Add(localDescarga);
        await db.SaveChangesAsync();

        var produto = TestDataSeeder.NovoProduto(empresaId, localDescarga.Id, localDescarga);
        db.Produto.Add(produto);
        await db.SaveChangesAsync();

        // Nenhum PlanejamentoRecebimento/ItemPlanejamento é criado para este fornecedor/produto
        // de propósito — é exatamente a condição que torna a reserva (e depois a confirmação) órfãs.

        var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
            empresaId,
            localDescarga.Id,
            unidade.Id,
            produto.Id,
            dataInicio: DateTime.UtcNow.AddHours(-1),
            dataFim: DateTime.UtcNow.AddHours(1),
            status: StatusAgendamento.EmAndamento);
        agendamento.FornecedorId = fornecedor.Id;
        db.Agendamento.Add(agendamento);
        await db.SaveChangesAsync();

        var reservaOrfa = RecebimentoEvento.CriarOrfao(
            produto.Id,
            fornecedor.Id,
            15m,
            agendamento.Id,
            "Reserva órfã: sem planejamento ativo no momento da reserva.",
            empresaId);
        db.RecebimentoEvento.Add(reservaOrfa);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService
        {
            UserId = Guid.NewGuid(),
            EmpresaId = empresaId,
            IsAdmin = true,
            IsMotorista = false,
        };

        var service = ServiceFactory.CreateAgendamentoAdminService(db, currentUser);

        await service.FinalizarAgendamento(agendamento.Id, quantidadeRecebida: 15m);

        using var assertDb = Fixture.CreateDbContext(empresaContext);

        var agendamentoFinal = await assertDb.Agendamento
            .SingleAsync(a => a.Id == agendamento.Id);
        Assert.Equal(StatusAgendamento.Finalizado, agendamentoFinal.StatusAgendamento);

        var eventos = await assertDb.RecebimentoEvento
            .Where(e => e.AgendamentoId == agendamento.Id)
            .ToListAsync();

        var evento = Assert.Single(eventos);
        Assert.Equal(TipoMovimentoRecebimento.Confirmacao, evento.Tipo);
        Assert.True(evento.EhOrfao);
        Assert.Equal(15m, evento.Quantidade);

        Assert.DoesNotContain(eventos, e => e.Tipo == TipoMovimentoRecebimento.Reserva);
    }
}
