using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class PlanejamentoRecebimentoService : IPlanejamentoRecebimentoService
    {
        private readonly IPlanejamentoRecebimentoRepositorio _planeRepo;
        private readonly IFornecedorRepositorio _forRepo;
        private readonly IValidator<RecebimentoCreateDto> _createValidator;
        private readonly IValidator<RecebimentoUpdateDto> _updateValidator;
        private readonly RecebimentoFactory _factory;
        private readonly IProdutoRepositorio _produtoRepositorio;

        public PlanejamentoRecebimentoService
            (
                IPlanejamentoRecebimentoRepositorio planeRepo,
                IValidator<RecebimentoCreateDto> createValidator,
                IValidator<RecebimentoUpdateDto> updateValidator,
                RecebimentoFactory factory,
                IFornecedorRepositorio repo,
                IProdutoRepositorio produtoRepositorio
            )
        {
            _planeRepo = planeRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _forRepo = repo;
            _produtoRepositorio = produtoRepositorio;
        }

        public async Task<RecebimentoResponseDto> CreateRecebimento
            (
                RecebimentoCreateDto recebimento,
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(recebimento, token);
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var novoRecebimento = await _factory.CreateRecebimentoFromDto(recebimento, token);

            var recebimentoCriado = await _planeRepo.CreateRecebimento(novoRecebimento, token);
            
            await _planeRepo.SaveChangesAsync(token);

            return MapToResponse(recebimentoCriado);
        }
      
        public async Task<RecebimentoResponseDto> GetById(Guid id, CancellationToken token = default)
        {
            var recebimento = await _planeRepo.GetById(id, token) 
                ?? throw new InvalidOperationException("Recebimento não encontrado");

            return MapToResponse(recebimento);
        }

        public async Task DeleteRecebimento(Guid id, CancellationToken token = default)
        {
            var recebimento = await _planeRepo.GetById(id, token) ?? 
                throw new InvalidOperationException("Recebimento não encontrado");

            await _planeRepo.DeleteRecebimento(recebimento.Id, token);
            await _planeRepo.SaveChangesAsync(token);
        }

        public async Task<RecebimentoResponseDto> UpdateRecebimento
            (
                Guid id,
                RecebimentoUpdateDto recebimento,
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _updateValidator.ValidateAsync(recebimento, token);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var recebimentoEncontrado = await _planeRepo.GetById(id, token)
                ?? throw new InvalidOperationException("recebimento não encontrado.");

            var fornecedor = await _forRepo.GetById(recebimento.FornecedorId, token)
                ?? throw new InvalidOperationException("Fornecedor não encontrado.");

            var produtosIds = recebimento.ItensPlanejamento!.Select(x => x.ProdutoId).ToList();
            var produtos = await _produtoRepositorio.GetByIdsAsync(produtosIds, token);


            recebimentoEncontrado.Fornecedor = fornecedor;
            recebimentoEncontrado.FornecedorId = fornecedor.Id;
            recebimentoEncontrado.DataInicio = recebimento.DataInicio;
            recebimentoEncontrado.StatusRecebimento = StatusRecebimento.Planejado;
            recebimentoEncontrado.UpdatedAt = DateTime.UtcNow;

            recebimentoEncontrado.ItemPlanejamentos = recebimento.ItensPlanejamento!
                .Select(x =>
                {
                    var produto = produtos.FirstOrDefault(p => p.Id == x.ProdutoId)
                        ?? throw new InvalidOperationException($"Produto com ID {x.ProdutoId} não encontrado.");

                    return new ItemPlanejamento
                    {
                        ProdutoId = produto.Id,
                        Produto = produto,
                        PlanejamentoRecebimentoId = recebimentoEncontrado.Id,
                        PlanejamentoRecebimento = recebimentoEncontrado,
                        QuantidadeTotalPlanejada = x.QuantidadeTotalPlanejada,
                        CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada
                    };
                })
                .ToList();

            var recebimentoAtualizado = await _planeRepo.UpdateRecebimento(id, recebimentoEncontrado, token);
            return MapToResponse(recebimentoAtualizado);
        }

        public async Task<List<RecebimentoResponseDto>> GetAll(CancellationToken token = default)
        {
            var listaRecebimento = await _planeRepo.GetAll(token);

            return listaRecebimento.Select(recebimento => new RecebimentoResponseDto
            {
                Id = recebimento.Id,
                DataInicio = recebimento.DataInicio,
                FornecedorNome = recebimento.Fornecedor.Nome,
                Itens = recebimento.ItemPlanejamentos.Select(item => new ItemPlanejamentoResponseDto
                {
                    Id = item.Id,
                    Produto = item.Produto!.Nome,
                    CadenciaDiariaPlanejada = item.CadenciaDiariaPlanejada,
                    QuantidadeTotalPlanejada = item.QuantidadeTotalPlanejada,
                    QuantidadeTotalRecebida = item.QuantidadeTotalRecebida,
                    FaltaReceber = item.QuantidadeTotalPlanejada - item.QuantidadeTotalRecebida,
                    Fornecedor = recebimento.Fornecedor.Nome
                }).ToList()
            }).ToList();
        }

        private static RecebimentoResponseDto MapToResponse(PlanejamentoRecebimento recebimento) =>
            new RecebimentoResponseDto
            {
                DataInicio = recebimento.DataInicio,
                Id = recebimento.Id,
                Itens = recebimento.ItemPlanejamentos!.Select(x => new ItemPlanejamentoResponseDto
                {
                    Id = x.Id,
                    CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada,
                    Produto = x.Produto!.Nome,
                    QuantidadeTotalPlanejada = x.QuantidadeTotalPlanejada,
                    QuantidadeTotalRecebida = x.QuantidadeTotalRecebida,
                    FaltaReceber = x.QuantidadeTotalPlanejada - x.QuantidadeTotalRecebida,
                    Fornecedor = recebimento.Fornecedor.Nome
                }).ToList()
            };
    }
}
