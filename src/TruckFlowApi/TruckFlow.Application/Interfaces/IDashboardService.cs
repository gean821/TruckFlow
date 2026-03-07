using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Dashboard;

namespace TruckFlow.Application.Interfaces
{
    public interface IDashboardService
    {
        public Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default);
    }
}
