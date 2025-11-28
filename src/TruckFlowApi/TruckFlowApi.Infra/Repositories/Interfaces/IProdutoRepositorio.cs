using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IProdutoRepositorio
    {
        public Task<List<Produto>> GetAll(CancellationToken cancellationToken = default);
        public Task<Produto> GetById(Guid id, CancellationToken cancellationToken = default);
        public Task<Produto> CreateProduto(Produto produto, CancellationToken cancellationToken = default);
        public Task<Produto> UpdateProduto(Guid id, Produto produto, CancellationToken cancellationToken = default);
        public Task DeleteProduto (Guid id, CancellationToken cancellationToken = default);
        public Task SaveChangesAsync(CancellationToken cancellation = default);

        public Task<List<Produto>> GetByIdsAsync(IEnumerable<Guid> produtoIds, CancellationToken token = default);
    }
}
