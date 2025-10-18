using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.UnidadeEntrega;

namespace TruckFlow.Application.Interfaces
{
    public interface IUnidadeEntregaService
    {
        public Task<List<UnidadeEntregaResponse>> GetAll(CancellationToken cancellationToken = default);
        public Task<UnidadeEntregaResponse> GetById(Guid id, CancellationToken cancellationToken = default);
        public Task<UnidadeEntregaResponse> CreateUnidadeEntrega(UnidadeEntregaCreateDto unidade, CancellationToken cancellationToken = default);
        public Task<UnidadeEntregaResponse> UpdateUnidadeEntrega(Guid id, UnidadeEntregaUpdateDto unidade, CancellationToken cancellationToken = default);
        public Task DeleteUnidadeEntrega(Guid id, CancellationToken cancellationToken = default);
    }
}
