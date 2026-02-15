using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class ItemPlanejamentoFactory
    {
        private readonly IPlanejamentoRecebimentoRepositorio _repo;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IEmpresaRepositorio _empresaRepo;

        public ItemPlanejamentoFactory(
            IPlanejamentoRecebimentoRepositorio repo,
            IProdutoRepositorio produtoRepo,
            IEmpresaRepositorio empresaRepo

            )
        {
            _repo = repo;
            _produtoRepo = produtoRepo;
            _empresaRepo = empresaRepo;
        }

        public async Task<ItemPlanejamento> CreateItemFromDto(
            ItemPlanejamentoCreateDto dto,
            PlanejamentoRecebimento? recebimentoPai,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var produto = await _produtoRepo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("Produto não encontrado.");

            PlanejamentoRecebimento? recebimento = recebimentoPai;

            // Se não foi passado o recebimento, busca pelo ID do DTO
            if (recebimento is null && dto.PlanejamentoRecebimentoId.HasValue)
            {
                recebimento = await _repo.GetById(dto.PlanejamentoRecebimentoId.Value, token)
                    ?? throw new NotFoundException("Recebimento não encontrado.");
            }

            var empresa = await _empresaRepo.GetById(dto.EmpresaId, token)
               ?? throw new NotFoundException("Empresa não encontrada.");

            var item = new ItemPlanejamento 
            {
                PlanejamentoRecebimento = recebimento!,
                PlanejamentoRecebimentoId = recebimento?.Id ?? dto.PlanejamentoRecebimentoId ?? Guid.Empty,
                Produto = produto,
                ProdutoId = dto.ProdutoId,
                CadenciaDiariaPlanejada = dto.CadenciaDiariaPlanejada,
                QuantidadeTotalPlanejada = dto.QuantidadeTotalPlanejada,
                CreatedAt = DateTime.UtcNow,
                Empresa = empresa
            };

            return item;
        }
    }
}
