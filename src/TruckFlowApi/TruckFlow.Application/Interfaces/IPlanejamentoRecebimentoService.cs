using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IPlanejamentoRecebimentoService
    {
        public Task<List<RecebimentoResponseDto>> GetAll(CancellationToken token = default);
        public Task<RecebimentoResponseDto> GetById(Guid id, CancellationToken token = default);
        public Task<RecebimentoResponseDto> CreateRecebimento
            (
                RecebimentoCreateDto recebimento,
                CancellationToken token = default
            );
        public Task DeleteRecebimento(Guid id, CancellationToken token = default);

        public Task<RecebimentoResponseDto> UpdateRecebimento
            (
                Guid id,
                RecebimentoUpdateDto recebimento,
                CancellationToken token = default
            );
    }
}
