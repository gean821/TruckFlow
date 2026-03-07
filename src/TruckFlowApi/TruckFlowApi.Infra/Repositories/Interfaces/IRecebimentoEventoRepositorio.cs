using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRecebimentoEventoRepositorio
    {
        Task<RecebimentoEvento> AddAsync(RecebimentoEvento evento, CancellationToken token = default);
        Task SaveChangeAsync(CancellationToken token = default);
    }
}
