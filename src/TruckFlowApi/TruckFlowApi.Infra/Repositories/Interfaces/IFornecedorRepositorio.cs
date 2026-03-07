using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IFornecedorRepositorio
    {
        public Task<Fornecedor?> GetById(Guid id, CancellationToken token = default);
        public Task<Fornecedor?> GetByCnpj(string cnpj, CancellationToken token = default);
        
        public Task<Fornecedor?> GetByNome(string nome, CancellationToken token = default);
        public Task<List<Fornecedor>> GetAll(CancellationToken token = default);
        public Task<Fornecedor> CreateFornecedor(Fornecedor fornecedor, CancellationToken token = default);
        public Task<Fornecedor> Update(Fornecedor fornecedor,  CancellationToken token = default);
        public Task<Fornecedor?> GetByIdWithProdutosAsync(Guid id, CancellationToken token = default);
        public Task Delete(Fornecedor fornecedor, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
        
    }
}
