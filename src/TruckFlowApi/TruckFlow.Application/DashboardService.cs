using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepositorio _repo;
        private readonly ILocalDescargaRepositorio _localDescargaRepo;

        public DashboardService(
            IDashboardRepositorio repo,
            ILocalDescargaRepositorio localDescargaRepo)
        {
            _repo = repo;
            _localDescargaRepo = localDescargaRepo;
        }

        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default)
        {
            var data = await _repo.GetDashboardSummaryAsync(token);

            var locais = await _localDescargaRepo.GetAll(token);
            var total = locais.Count;
            var livres = locais.Count(x => x.Ativa == true);
            var ocupadas = locais.Count(x => x.Ativa != true);

            data.Docas.Total = total;
            data.Docas.Livres = livres;
            data.Docas.Ocupadas = ocupadas;
            data.Docas.OcupacaoPorcentagem = total > 0
                ? (int)Math.Round((double)ocupadas / total * 100)
                : 0;

            decimal metaDiaria = 500000;
            if (metaDiaria > 0)
            {
                int progresso = (int)((data.Volume.TotalKg / metaDiaria) * 100);
                data.Volume.ProgressoDiario = Math.Min(progresso, 100);
            }

            return data;
        }
    }
}