using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Application.Interfaces
{
    public interface IFornecedorService
    {
        public Task<FornecedorResponse> GetById(Guid id, CancellationToken token = default);
        public Task<List<FornecedorResponse>> GetAll(CancellationToken token = default);
        public Task<FornecedorResponse> CreateFornecedor
            (
                FornecedorCreateDto fornecedor,
                CancellationToken token = default
            );
        public Task<FornecedorResponse> UpdateFornecedor
            (
                Guid id,
                FornecedorUpdateDto fornecedor,
                CancellationToken token = default
            );
        public Task DeleteFornecedor(Guid id, CancellationToken token = default);

        public Task<List<FornecedorResponse>> GetByIdWithProdutosAsync
            (
                IEnumerable<Guid> produtoIds,
                CancellationToken token = default
            );

        public Task<FornecedorResponse> GetByNome(string nome, CancellationToken token = default);

        public Task<FornecedorResponse> AddProdutoToFornecedorAsync
            (
                Guid fornecedorId,
                Guid produtoId,
                CancellationToken token = default
            );
        public Task<FornecedorResponse> DeleteProdutoFromFornecedorAsync
            (
                Guid fornecedorId,
                Guid produtoId,
                CancellationToken token = default
            );
    }
}
