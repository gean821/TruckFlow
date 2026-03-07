using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepositorio _repo;

        public DashboardService(IDashboardRepositorio repo)
        {
            _repo = repo;
        }

        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default)
        {
            // 1. Busca os dados brutos do repositório
            var data = await _repo.GetDashboardSummaryAsync(token);

            // 2. Aplica Regra de Negócio (Ex: Cálculo de Meta)
            // Supondo que a meta seja 500 Toneladas (500.000kg)
            decimal metaDiaria = 500000;

            if (metaDiaria > 0)
            {
                int progresso = (int)((data.Volume.TotalKg / metaDiaria) * 100);
                data.Volume.ProgressoDiario = Math.Min(progresso, 100); // Trava em 100% visualmente
            }
            return data;
        }
    }
}
