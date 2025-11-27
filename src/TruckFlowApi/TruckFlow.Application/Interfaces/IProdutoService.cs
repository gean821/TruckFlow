using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Application.Interfaces
{
    public interface IProdutoService
    {
        public Task<List<ProdutoResponse>> GetAll(CancellationToken cancellationToken = default);
        public Task<ProdutoResponse> GetById(Guid id, CancellationToken cancellationToken = default);
        public Task<ProdutoResponse> CreateProduto(ProdutoCreateDto produto, CancellationToken cancellationToken = default);
        public Task<ProdutoResponse> UpdateProduto(Guid id, ProdutoEditDto produto, CancellationToken cancellationToken = default);
        public Task DeleteProduto(Guid id, CancellationToken cancellationToken = default);
    }
}
