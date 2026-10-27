using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepositorio _repo;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly CurrentUserGuard _guard;
        private readonly TimeProvider _timeProvider;

        public DashboardService(
            IDashboardRepositorio repo,
            IEmpresaRepositorio empresaRepo,
            CurrentUserGuard guard,
            TimeProvider timeProvider)
        {
            _repo = repo;
            _empresaRepo = empresaRepo;
            _guard = guard;
            _timeProvider = timeProvider;
        }

        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default)
        {
            var agoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, Grade.OperationalTimeZone).Date;
            var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(hojeLocal, Grade.OperationalTimeZone);
            var fimDiaUtc = TimeZoneInfo.ConvertTimeToUtc(hojeLocal.AddDays(1), Grade.OperationalTimeZone);

            var data = await _repo.GetDashboardSummaryAsync(inicioDiaUtc, fimDiaUtc, agoraUtc, token);

            var empresa = await _empresaRepo.GetById(_guard.GetEmpresaId(), token);
            var metaDiariaKg = EmpresaMetaDiaria.ResolveKg(empresa?.Configuracoes);

            data.Volume.MetaDiariaKg = metaDiariaKg;
            data.Volume.ProgressoDiario = metaDiariaKg is > 0
                ? Math.Min((int)(data.Volume.TotalKg / metaDiariaKg.Value * 100), 100)
                : 0;

            return data;
        }
    }
}
