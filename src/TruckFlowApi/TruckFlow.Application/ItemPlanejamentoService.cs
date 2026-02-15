using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
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
        private readonly IRecebimentoEventoRepositorio _eventoRepo;
        private readonly IUsuarioService _usuarioService;

        public ItemPlanejamentoService(
            IItemPlanejamentoRepositorio repo,
            IValidator<ItemPlanejamentoCreateDto> createValidator,
            IValidator<ItemPlanejamentoUpdateDto> updateValidator,
            ItemPlanejamentoFactory factory,
            IProdutoRepositorio produtoRepo,
            IRecebimentoEventoRepositorio eventoRepo,
            IUsuarioService usuarioService)
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _produtoRepo = produtoRepo;
            _eventoRepo = eventoRepo;
            _usuarioService = usuarioService;
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

            var novoItem = await _factory.CreateItemFromDto(dto, null, token);
            var itemCriado = await _repo.CreateItem(novoItem, token);

            return MapToResponse(itemCriado, token);
        }

        public async Task DeleteItem(Guid id, CancellationToken token = default)
        {
            var item = await _repo.GetById(id, token)
                ?? throw new NotFoundException("item não encontrado");

            var planejamentoId = item.PlanejamentoRecebimentoId;
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
                Fornecedor = x.PlanejamentoRecebimento!.Fornecedor.Nome,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        public async Task<ItemPlanejamentoResponseDto> GetById(Guid id, CancellationToken token = default)
        {
            var item = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Item não encontrado");

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
                ?? throw new NotFoundException("item não encontrado");

            if (item.ProdutoId != dto.ProdutoId) {
                
                var produto = await _produtoRepo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("produto não encontrado para ser atualizado");
                item.Produto = produto;
                item.ProdutoId = produto.Id;
            }

            item.CadenciaDiariaPlanejada = dto.CadenciaDiariaPlanejada;
            item.QuantidadeTotalPlanejada = dto.QuantidadeTotalPlanejada;
            item.UpdatedAt = DateTime.UtcNow;

            var itemAtualizado = await _repo.UpdateItem(id, item, token);
            return MapToResponse(itemAtualizado, token);
        }

        public async Task RegistrarRecebimentoManual(
                Guid itemPlanejamentoId,
                decimal quantidade,
                string? observacao,
                Usuario user,
                CancellationToken token = default)
        {
            var item = await _repo.GetByIdWithPlanejamento(itemPlanejamentoId, token)
                ?? throw new NotFoundException("Item não encontrado.");

            var usuario = await _usuarioService.GetAdminByIdAsync(user.Id, token)
                ?? throw new NotFoundException("Usuário não encontrado.");

            var empresa = usuario.EmpresaId;
            
            var evento = new RecebimentoEvento(
                item,
                quantidade,
                agendamentoId: null,
                observacao,
                empresa
            );

            await _eventoRepo.AddAsync(evento, token);

            // 2️⃣ Atualiza domínio
            item.RegistrarRecebimento(quantidade);
            item.PlanejamentoRecebimento.RecalcularStatus();
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
                FaltaReceber = item.QuantidadeTotalPlanejada - item.QuantidadeTotalRecebida,
                CreatedAt = DateTime.UtcNow
            };
    }
}