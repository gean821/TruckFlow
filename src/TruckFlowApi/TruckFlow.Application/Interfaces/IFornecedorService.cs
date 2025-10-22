using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Application.Interfaces
{
    public interface IFornecedorService
    {
        public Task<FornecedorResponse> GetById(Guid id, CancellationToken token = default);
        public Task<List<FornecedorResponse>> GetAll(CancellationToken token = default);
        public Task<FornecedorResponse> CreateFornecedor(FornecedorCreateDto fornecedor, CancellationToken token = default);
        public Task<FornecedorResponse> UpdateFornecedor(Guid id, FornecedorUpdateDto fornecedor, CancellationToken token = default);
        public Task DeleteFornecedor(Guid id, CancellationToken token = default);

        public Task<FornecedorResponse> GetByNome(string nome, CancellationToken token = default);
    }
}
