using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IPlanejamentoRecebimentoRepositorio
    {
        public Task<List<PlanejamentoRecebimento>> GetAll(CancellationToken token = default);
        public Task<PlanejamentoRecebimento> GetById (Guid id, CancellationToken token = default);
        public Task<PlanejamentoRecebimento> CreateRecebimento
            (
                PlanejamentoRecebimento recebimento,
                CancellationToken token = default
            );
        public Task DeleteRecebimento(Guid id, CancellationToken token = default);

        public Task<PlanejamentoRecebimento> UpdateRecebimento
            (
                Guid id,
                PlanejamentoRecebimento recebimento,
                CancellationToken token = default
            );
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
