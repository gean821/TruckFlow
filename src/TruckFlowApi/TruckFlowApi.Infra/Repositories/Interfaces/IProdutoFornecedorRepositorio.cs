using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IProdutoFornecedorRepositorio //vai ser usada apenas futuramente, caso aqui tenha uma regra de negócio especifica a ser usada.
    {
        public Task<ProdutoFornecedor> Add(Produto produto, Fornecedor fornecedor, CancellationToken token);
        public Task<List<ProdutoFornecedor>> GetAll(CancellationToken token);
        public Task<List<ProdutoFornecedor>> GetById(Guid produtoId, Guid fornecedorId, CancellationToken token);
        public Task<List<ProdutoFornecedor>> Update
            (
                Guid produtoId,
                Produto produto,
                Guid fornecedorId,
                Fornecedor fornecedor,
                CancellationToken token
            );
        public Task<List<ProdutoFornecedor>> Delete
            (
                Guid produtoId,
                Guid fornecedorId,
                CancellationToken token
            );
    }
}
