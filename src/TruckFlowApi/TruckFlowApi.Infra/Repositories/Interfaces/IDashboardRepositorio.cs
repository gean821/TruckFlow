using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Dashboard;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IDashboardRepositorio
    {
        Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default);
    }
}
