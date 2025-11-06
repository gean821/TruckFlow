using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class ItemPlanejamentoService : IItemPlanejamentoService
    {

        private readonly IItemPlanejamentoRepositorio _repo;
        private readonly IValidator<ItemPlanejamentoCreateDto> _createValidator;
        private readonly IValidator<ItemPlanejamentoUpdateDto> _updateValidator;
        private readonly ItemPlanejamentoFactory _factory;
        private readonly IProdutoRepositorio _produtoRepo;

        public ItemPlanejamentoService
            (
                IItemPlanejamentoRepositorio repo,
                IValidator<ItemPlanejamentoCreateDto> createValidator,
                IValidator<ItemPlanejamentoUpdateDto> updateValidator,
                ItemPlanejamentoFactory factory,
                IProdutoRepositorio produtoRepo
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _produtoRepo = produtoRepo;
        }

        public async Task<ItemPlanejamentoResponseDto> CreateItem
            (
                ItemPlanejamentoCreateDto dto,
                CancellationToken token = default
            )
        {
            ValidationResult validation = await _createValidator.ValidateAsync(dto, token);
            
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var novoItem = await _factory.CreateItemFromDto(dto, token);
            var itemCriado = await _repo.CreateItem(novoItem, token);

            return MapToResponse(itemCriado, token);
        }

        public async Task DeleteItem(Guid id, CancellationToken token = default)
        {
            var item = await _repo.GetById(id, token)
                ?? throw new InvalidOperationException("item não encontrado");

            await _repo.DeleteItem(item.Id, token);
        }

        public async Task<List<ItemPlanejamentoResponseDto>> GetAll(CancellationToken token = default)
        {
            var itens = await _repo.GetAll(token);

            return itens.Select(x => new ItemPlanejamentoResponseDto
            {
                Id = x.Id,
                Produto = x.Produto!.Nome,
                CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada,
                QuantidadeTotalPlanejada = x.QuantidadeTotalPlanejada,
                QuantidadeTotalRecebida = x.QuantidadeTotalRecebida,
                FaltaReceber = x.QuantidadeTotalPlanejada - x.QuantidadeTotalRecebida,
                Fornecedor = x.PlanejamentoRecebimento!.Fornecedor.Nome
            }).ToList();
        }

        public async Task<ItemPlanejamentoResponseDto> GetById(Guid id, CancellationToken token = default)
        {
            var item = await _repo.GetById(id, token)
                ?? throw new InvalidOperationException("Item não encontrado");

            return MapToResponse(item, token);
        }

        public async Task<ItemPlanejamentoResponseDto> UpdateItem
            (   Guid id,
                ItemPlanejamentoUpdateDto dto,
                CancellationToken token = default
            )
        {

            ValidationResult validation = await _updateValidator.ValidateAsync(dto, token);
            
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var item = await _repo.GetById(id, token)
                ?? throw new InvalidOperationException("item não encontrado");

            if (item.ProdutoId != dto.ProdutoId) {
                
                var produto = await _produtoRepo.GetById(dto.ProdutoId, token)
                ?? throw new InvalidOperationException("produto não encontrado para ser atualizado");
                item.Produto = produto;
                item.ProdutoId = produto.Id;
            }

            item.CadenciaDiariaPlanejada = dto.CadenciaDiariaPlanejada;
            item.QuantidadeTotalPlanejada = dto.QuantidadeTotalPlanejada;
            item.UpdatedAt = DateTime.UtcNow;

            var itemAtualizado = await _repo.UpdateItem(id, item, token);
            return MapToResponse(itemAtualizado, token);
        }

        private static ItemPlanejamentoResponseDto MapToResponse(ItemPlanejamento item, CancellationToken token = default) =>
            new ItemPlanejamentoResponseDto
            {
                Id = item.Id,
                CadenciaDiariaPlanejada = item.CadenciaDiariaPlanejada,
                Produto = item.Produto!.Nome,
                QuantidadeTotalPlanejada = item.QuantidadeTotalPlanejada,
                QuantidadeTotalRecebida = item.QuantidadeTotalRecebida,
                Fornecedor = item.PlanejamentoRecebimento!.Fornecedor.Nome,
                FaltaReceber = item.QuantidadeTotalPlanejada - item.QuantidadeTotalRecebida
            };
    }
}