using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories;

namespace TruckFlow.Test.Integration.Fixtures;

internal static class ServiceFactory
{
    public static IEmpresaContext NewEmpresaContext() => new EmpresaContext(new HttpContextAccessor());

    public static IAgendamentoMotoristaService CreateAgendamentoMotoristaService(
        AppDbContext db,
        IEmpresaContext empresaContext,
        ICurrentUserService currentUser)
    {
        var agendamentoRepo = new AgendamentoRepositorio(db);
        var notaRepo = new NotaFiscalRepositorio(db);
        var planejamentoRepo = new PlanejamentoRecebimentoRepositorio(db);
        var eventoRepo = new RecebimentoEventoRepositorio(db);

        var lifecycle = new AgendamentoRecebimentoLifecycleService(
            planejamentoRepo,
            eventoRepo,
            NullLogger<AgendamentoRecebimentoLifecycleService>.Instance);

        var guard = new CurrentUserGuard(currentUser);

        return new AgendamentoMotoristaService(
            agendamentoRepo,
            notaRepo,
            planejamentoRepo,
            lifecycle,
            empresaContext,
            NullLogger<AgendamentoMotoristaService>.Instance,
            guard);
    }
}
