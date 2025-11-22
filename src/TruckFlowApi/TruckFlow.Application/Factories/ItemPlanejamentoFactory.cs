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

        public async Task<ItemPlanejamento> CreateItemFromDto(
            ItemPlanejamentoCreateDto dto,
            PlanejamentoRecebimento? recebimentoPai,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var produto = await _produtoRepo.GetById(dto.ProdutoId)
                ?? throw new InvalidOperationException("Produto não encontrado.");

            PlanejamentoRecebimento? recebimento = recebimentoPai;

            // Se não foi passado o recebimento, busca pelo ID do DTO
            if (recebimento is null && dto.PlanejamentoRecebimentoId.HasValue)
            {
                recebimento = await _repo.GetById(dto.PlanejamentoRecebimentoId.Value, token)
                    ?? throw new InvalidOperationException("Recebimento não encontrado.");
            }

            var item = new ItemPlanejamento 
            {
                PlanejamentoRecebimento = recebimento!,
                PlanejamentoRecebimentoId = recebimento?.Id ?? dto.PlanejamentoRecebimentoId ?? Guid.Empty,
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
