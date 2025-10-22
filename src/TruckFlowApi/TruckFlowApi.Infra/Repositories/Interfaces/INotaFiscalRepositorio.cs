using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotaFiscalRepositorio
    {
        Task<NotaFiscal> SaveParsedNotaAsync(NotaFiscal nota, CancellationToken token);
        public Task SaveChangesAsync(CancellationToken token);

        Task<NotaFiscal?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token);
    }
}
