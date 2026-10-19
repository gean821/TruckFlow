using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.AgendamentoMotorista;
using TruckFlow.Application.Validators.Grade;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Test.Integration.Fixtures;

internal static class ServiceFactory
{
    public static IEmpresaContext NewEmpresaContext() => new EmpresaContext(new HttpContextAccessor());

    public static IGradeService CreateGradeService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IAgendamentoRepositorio? agendamentoRepo = null)
    {
        var gradeRepo = new GradeRepositorio(db);
        var produtoRepo = new ProdutoRepositorio(db);
        var fornecedorRepo = new FornecedorRepositorio(db);
        var unidadeRepo = new UnidadeEntregaRepositorio(db);
        var agendamentoRepositorio = agendamentoRepo ?? new AgendamentoRepositorio(db);
        var localDescargaRepo = new LocalDescargaRepositorio(db);
        var guard = new CurrentUserGuard(currentUser);

        return new GradeService(
            gradeRepo,
            new GradeCreateValidator(),
            new GradeUpdateValidator(),
            produtoRepo,
            fornecedorRepo,
            unidadeRepo,
            agendamentoRepositorio,
            localDescargaRepo,
            guard,
            NullLogger<GradeService>.Instance);
    }

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

    public static IAgendamentoAdminService CreateAgendamentoAdminService(
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        var agendamentoRepo = new AgendamentoRepositorio(db);
        var localDescargaRepo = new LocalDescargaRepositorio(db);
        var fornecedorRepo = new FornecedorRepositorio(db);
        var unidadeRepo = new UnidadeEntregaRepositorio(db);
        var empresaRepo = new EmpresaRepositorio(db);
        var produtoRepo = new ProdutoRepositorio(db);
        var planejamentoRepo = new PlanejamentoRecebimentoRepositorio(db);
        var eventoRepo = new RecebimentoEventoRepositorio(db);

        var lifecycle = new AgendamentoRecebimentoLifecycleService(
            planejamentoRepo,
            eventoRepo,
            NullLogger<AgendamentoRecebimentoLifecycleService>.Instance);

        return new AgendamentoAdminService(
            agendamentoRepo,
            new AgendamentoAdminCreateValidator(),
            new AgendamentoAdminUpdateDtoValidator(),
            localDescargaRepo,
            fornecedorRepo,
            unidadeRepo,
            lifecycle,
            empresaRepo,
            currentUser,
            produtoRepo,
            localDescargaRepo,
            NullLogger<AgendamentoAdminService>.Instance);
    }
}
