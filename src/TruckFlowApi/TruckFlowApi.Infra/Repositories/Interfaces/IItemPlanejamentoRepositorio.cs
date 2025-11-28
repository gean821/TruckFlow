using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IItemPlanejamentoRepositorio
    {
        public Task<List<ItemPlanejamento>> GetAll(CancellationToken token = default);

        public Task<ItemPlanejamento> GetById(Guid id, CancellationToken token = default);

        public Task<ItemPlanejamento> CreateItem
            (
                ItemPlanejamento item,
                CancellationToken token = default
            );
        public Task DeleteItem(Guid id, CancellationToken token = default);

        public Task<ItemPlanejamento> UpdateItem
            (
                Guid id,
                ItemPlanejamento item,
                CancellationToken token = default
            );
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
