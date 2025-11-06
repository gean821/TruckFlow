using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class ItemPlanejamentoFactory
    {
        private readonly IPlanejamentoRecebimentoRepositorio _repo;
        private readonly IProdutoRepositorio _produtoRepo;
        public ItemPlanejamentoFactory(
            IPlanejamentoRecebimentoRepositorio repo,
            IProdutoRepositorio produtoRepo
            )
        {
            _repo = repo;
            _produtoRepo = produtoRepo;
        }

        public async Task<ItemPlanejamento> CreateItemFromDto(ItemPlanejamentoCreateDto dto, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(dto);
            
            var recebimento = await _repo.GetById(dto.PlanejamentoRecebimentoId, token)
                ?? throw new InvalidOperationException("recebimento não encontrado.");

            var produto = await _produtoRepo.GetById(dto.ProdutoId)
                ?? throw new InvalidOperationException("Produto não encontrado.");

            var item = new ItemPlanejamento 
            {
                PlanejamentoRecebimento = recebimento,
                PlanejamentoRecebimentoId = dto.PlanejamentoRecebimentoId,
                Produto = produto,
                ProdutoId = dto.ProdutoId,
                CadenciaDiariaPlanejada = dto.CadenciaDiariaPlanejada,
                QuantidadeTotalPlanejada = dto.QuantidadeTotalPlanejada,
                CreatedAt = DateTime.UtcNow,
            };

            return item;
        }
    }
}
