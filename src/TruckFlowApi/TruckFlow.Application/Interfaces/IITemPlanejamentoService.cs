using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IItemPlanejamentoService
    {
        public Task<List<ItemPlanejamentoResponseDto>> GetAll(CancellationToken token = default);
        public Task<ItemPlanejamentoResponseDto> GetById(Guid id, CancellationToken token = default);
        public Task<ItemPlanejamentoResponseDto> CreateItem
            (
                ItemPlanejamentoCreateDto item,
            CancellationToken token = default);
        public Task DeleteItem(Guid id, CancellationToken token = default);

        public Task<ItemPlanejamentoResponseDto> UpdateItem
            (
                Guid id,
                ItemPlanejamentoUpdateDto ItemPlanejamentoResponseDto,
                CancellationToken token = default
            );
    }
}

