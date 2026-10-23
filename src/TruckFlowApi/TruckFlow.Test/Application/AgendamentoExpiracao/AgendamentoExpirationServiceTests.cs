using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Application.AgendamentoExpiracao.Fakes;
using AgendamentoEntity = TruckFlow.Domain.Entities.Agendamento;

namespace TruckFlow.Test.Application.AgendamentoExpiracao;

public class AgendamentoExpirationServiceTests
{
    [Fact]
    public async Task ExpirarVencidosAsync_LockNaoAdquirida_NaoBuscaCandidatosEComita()
    {
        var repo = new FakeAgendamentoRepositorioParaExpiracao { LockAdquirido = false };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);
        var empresaId = Guid.NewGuid();

        var total = await service.ExpirarVencidosAsync(empresaId, CancellationToken.None);

        Assert.Equal(0, total);
        Assert.Equal(empresaId, repo.EmpresaIdRecebidaNaLock);
        Assert.False(repo.SaveChangesFoiChamado);
        Assert.True(repo.UltimaTransacao!.Comitada);
        Assert.False(repo.UltimaTransacao.Revertida);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_LockAdquiridaSemCandidatos_ComitaERetornaZero()
    {
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, total);
        Assert.False(repo.SaveChangesFoiChamado);
        Assert.True(repo.UltimaTransacao!.Comitada);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_CandidatoDisponivelVencido_ExpiraENaoChamaEstorno()
    {
        var agendamento = CriarAgendamentoVencido(StatusAgendamento.Disponivel);
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [agendamento],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Equal(StatusAgendamento.Expirado, agendamento.StatusAgendamento);
        Assert.False(lifecycle.AoCancelarOuExpirarFoiChamado);
        Assert.True(repo.SaveChangesFoiChamado);
        Assert.True(repo.UltimaTransacao!.Comitada);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_CandidatoAgendadoVencido_ExpiraEChamaEstornoDoLifecycle()
    {
        var agendamento = CriarAgendamentoVencido(StatusAgendamento.Agendado);
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [agendamento],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Equal(StatusAgendamento.Expirado, agendamento.StatusAgendamento);
        Assert.True(lifecycle.AoCancelarOuExpirarFoiChamado);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_EstornoDoLifecycleFalha_ContinuaEContabilizaMesmoAssim()
    {
        var agendamento = CriarAgendamentoVencido(StatusAgendamento.Agendado);
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [agendamento],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService { LancarExceptionNoEstorno = true };
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Equal(StatusAgendamento.Expirado, agendamento.StatusAgendamento);
        Assert.True(repo.SaveChangesFoiChamado);
        Assert.True(repo.UltimaTransacao!.Comitada);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_ConflitoDeConcorrenciaNoSave_RevertaENaoRelanca()
    {
        var agendamento = CriarAgendamentoVencido(StatusAgendamento.Disponivel);
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [agendamento],
            LancarConcurrencyExceptionNoSave = true,
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, total);
        Assert.True(repo.UltimaTransacao!.Revertida);
        Assert.False(repo.UltimaTransacao.Comitada);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_LoteComMultiplosCandidatos_ProcessaTodosEEstornaSoOsAgendados()
    {
        var disponivel = CriarAgendamentoVencido(StatusAgendamento.Disponivel);
        var pendente = CriarAgendamentoVencido(StatusAgendamento.Pendente);
        var agendado1 = CriarAgendamentoVencido(StatusAgendamento.Agendado);
        var agendado2 = CriarAgendamentoVencido(StatusAgendamento.Agendado);

        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [disponivel, pendente, agendado1, agendado2],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(4, total);
        Assert.All(
            [disponivel, pendente, agendado1, agendado2],
            a => Assert.Equal(StatusAgendamento.Expirado, a.StatusAgendamento));
        Assert.True(lifecycle.AoCancelarOuExpirarFoiChamado);
        Assert.Equal(2, lifecycle.ChamadasDeEstorno);
    }

    [Fact]
    public async Task ExpirarVencidosAsync_CandidatoQueNaoPodeExpirarNaData_EhIgnorado()
    {
        var naoExpiravel = new AgendamentoEntity
        {
            EmpresaId = Guid.NewGuid(),
            TipoCarga = TipoCarga.Milho,
            StatusAgendamento = StatusAgendamento.EmAndamento,
            DataInicio = DateTime.UtcNow.AddHours(-3),
            DataFim = DateTime.UtcNow.AddHours(-1),
        };
        var repo = new FakeAgendamentoRepositorioParaExpiracao
        {
            LockAdquirido = true,
            Candidatos = [naoExpiravel],
        };
        var lifecycle = new FakeAgendamentoRecebimentoLifecycleService();
        var service = CriarService(repo, lifecycle);

        var total = await service.ExpirarVencidosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, total);
        Assert.Equal(StatusAgendamento.EmAndamento, naoExpiravel.StatusAgendamento);
        Assert.False(lifecycle.AoCancelarOuExpirarFoiChamado);
        Assert.True(repo.SaveChangesFoiChamado);
        Assert.True(repo.UltimaTransacao!.Comitada);
    }

    private static AgendamentoExpirationService CriarService(
        FakeAgendamentoRepositorioParaExpiracao repo,
        FakeAgendamentoRecebimentoLifecycleService lifecycle) =>
        new(repo, lifecycle, NullLogger<AgendamentoExpirationService>.Instance);

    private static AgendamentoEntity CriarAgendamentoVencido(StatusAgendamento status) =>
        new()
        {
            EmpresaId = Guid.NewGuid(),
            TipoCarga = TipoCarga.Milho,
            StatusAgendamento = status,
            DataInicio = DateTime.UtcNow.AddHours(-3),
            DataFim = DateTime.UtcNow.AddHours(-1),
        };
}
