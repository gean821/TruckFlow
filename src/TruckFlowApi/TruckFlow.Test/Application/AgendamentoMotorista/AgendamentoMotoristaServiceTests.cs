using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Application.AgendamentoMotorista.Fakes;
using AgendamentoEntity = TruckFlow.Domain.Entities.Agendamento;

namespace TruckFlow.Test.Application.AgendamentoMotorista;

public class AgendamentoMotoristaServiceTests
{
    [Fact]
    public async Task GetDriverAppointments_SemAgendamentos_RetornaListaVaziaSemLancarExcecao()
    {
        var motoristaId = Guid.NewGuid();
        var repo = new FakeAgendamentoRepositorioParaMotorista
        {
            AgendamentosDoMotorista = [],
        };
        var service = CriarService(repo, motoristaId);

        var resultado = await service.GetDriverAppointments(CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Empty(resultado);
        Assert.Equal(motoristaId, repo.MotoristaIdRecebido);
    }

    [Fact]
    public async Task GetDriverAppointments_ComAgendamentos_RetornaListaMapeada()
    {
        var motoristaId = Guid.NewGuid();
        var agendamento = new AgendamentoEntity
        {
            EmpresaId = Guid.NewGuid(),
            TipoCarga = TipoCarga.Milho,
            StatusAgendamento = StatusAgendamento.Agendado,
            DataInicio = DateTime.UtcNow.AddHours(2),
            DataFim = DateTime.UtcNow.AddHours(3),
            CreatedAt = DateTime.UtcNow,
        };
        var repo = new FakeAgendamentoRepositorioParaMotorista
        {
            AgendamentosDoMotorista = [agendamento],
        };
        var service = CriarService(repo, motoristaId);

        var resultado = await service.GetDriverAppointments(CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(agendamento.Id, resultado[0].Id);
        Assert.Equal(StatusAgendamento.Agendado.ToString(), resultado[0].Status);
    }

    private static AgendamentoMotoristaService CriarService(
        FakeAgendamentoRepositorioParaMotorista repo,
        Guid motoristaId)
    {
        var currentUser = new FakeCurrentUserService { MotoristaId = motoristaId };
        var guard = new CurrentUserGuard(currentUser);

        return new AgendamentoMotoristaService(
            repo,
            new FakeNotaFiscalRepositorio(),
            new FakePlanejamentoRecebimentoRepositorio(),
            new FakeAgendamentoRecebimentoLifecycleService(),
            new FakeEmpresaContext(),
            NullLogger<AgendamentoMotoristaService>.Instance,
            guard);
    }
}
