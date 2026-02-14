using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RecebimentoService : IRecebimentoEventoService
    {
        private readonly IItemPlanejamentoRepositorio _itemRepo;
        private readonly IRecebimentoEventoRepositorio _eventoRepo;

        public RecebimentoService
            (
            IItemPlanejamentoRepositorio itemRepo,
            IRecebimentoEventoRepositorio eventoRepo
            )
        {
            _itemRepo = itemRepo;
            _eventoRepo = eventoRepo;
        }

        public async Task RegistrarRecebimentoManual(
         Guid itemPlanejamentoId,
         decimal quantidade,
         string? observacao,
         CancellationToken token
     )
        {
            var item = await _itemRepo.GetByIdWithPlanejamento(itemPlanejamentoId, token)
                ?? throw new NotFoundException("Item do planejamento não encontrado.");

            var evento = new RecebimentoEvento(
                item,
                quantidade,
                agendamentoId: null,
                observacao
            );
            
            await _eventoRepo.AddAsync(evento, token);
            item.RegistrarRecebimento(quantidade);
            item.PlanejamentoRecebimento.RecalcularStatus();
        }
    }
}

