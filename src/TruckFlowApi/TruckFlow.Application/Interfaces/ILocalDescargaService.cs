using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.LocalDescarga;

namespace TruckFlow.Application.Interfaces
{
    public interface ILocalDescargaService
    {
        public Task<LocalDescargaResponse> GetById(
            Guid id,
            CancellationToken token = default);
        public Task<List<LocalDescargaResponse>> GetAll(CancellationToken token = default);
        public Task<LocalDescargaResponse> CreateLocalDescarga(
            LocalDescargaCreateDto local,
            CancellationToken token = default);
        public Task<LocalDescargaResponse> Update(
            Guid id,
            LocalDescargaUpdateDto local,
            CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
    }
}
