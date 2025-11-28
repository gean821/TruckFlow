using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class FornecedorService : IFornecedorService
    {
        private readonly IFornecedorRepositorio _repo;
        private readonly IValidator<FornecedorCreateDto> _createValidator;
        private readonly IValidator<FornecedorUpdateDto> _updateValidator;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly FornecedorFactory _factory;

        public FornecedorService(
            IFornecedorRepositorio repo,
            IValidator<FornecedorCreateDto> createValidator,
            IValidator<FornecedorUpdateDto> updateValidator,
            FornecedorFactory factory,
            IProdutoRepositorio produtoRepo
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _produtoRepo = produtoRepo;
        }

        public async Task<FornecedorResponse> CreateFornecedor
            (
                FornecedorCreateDto fornecedor,
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(fornecedor, token);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            List<Produto> produtos = new();

            if (fornecedor.ProdutoIds?.Count > 0)
            {
                produtos = await _produtoRepo.GetByIdsAsync(fornecedor.ProdutoIds, token);
            }
            
            var fornecedorCriado = await _factory.CreateFornecedorFromDto(fornecedor, produtos, token);

            await _repo.CreateFornecedor(fornecedorCriado, token);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedorCriado);
        }

        public async Task<FornecedorResponse> GetById(Guid id, CancellationToken cancellatioToken = default)
        {
            var fornecedorEncontrado = await _repo.GetById(id, cancellatioToken)
                ?? throw new ArgumentNullException("Fornecedor não encontrado");

            return new FornecedorResponse
            {
                Id = fornecedorEncontrado.Id,
                Nome = fornecedorEncontrado.Nome,
                CreatedAt = fornecedorEncontrado.CreatedAt
            };
        }
        public async Task DeleteFornecedor(Guid id, CancellationToken cancellationToken = default)
        {
            var fornecedorEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Fornecedor não encontrado");

            await _repo.Delete(fornecedorEncontrado.Id, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<FornecedorResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listaFornecedor = await _repo.GetAll(cancellationToken);

            if (listaFornecedor.Count == 0)
            {
                return new List<FornecedorResponse>();
            }

            return MapListToResponse(listaFornecedor);
        }

        public async Task<FornecedorResponse> UpdateFornecedor
            (
                Guid id,
                FornecedorUpdateDto fornecedor,
                CancellationToken cancellationToken = default
           )

        {
            ValidationResult validationResult = await _updateValidator.ValidateAsync(fornecedor, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var fornecedorEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Fornecedor não encontrado.");

            fornecedorEncontrado.Nome = fornecedor.Nome;
            
            var fornecedorAtualizado = await _repo.Update(id, fornecedorEncontrado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return MapToResponse(fornecedorAtualizado);
        }
        public async Task<FornecedorResponse> GetByNome(string nome, CancellationToken token = default)
        {
            var fornecedorEncontrado = await _repo.GetByNome(nome, token)
               ?? throw new ArgumentNullException("Fornecedor não encontrado");

            return MapToResponse(fornecedorEncontrado);
        }
        public async Task<FornecedorResponse> GetByIdWithProdutosAsync
            (
               Guid id,
               CancellationToken token = default
            )
        {
            var fornecedorEncontrado = await _repo.GetById(id, token)
               ?? throw new ArgumentNullException("Fornecedor não encontrado");

            return MapToResponse(fornecedorEncontrado);
        }
        public async Task<FornecedorResponse> AddProdutoToFornecedorAsync
          (
            Guid fornecedorId,
            Guid produtoId,
            CancellationToken token = default
          )

        {
            var fornecedor = await _repo.GetByIdWithProdutosAsync(fornecedorId, token)
                          ?? throw new ArgumentNullException("Fornecedor não encontrado");

            var produto = await _produtoRepo.GetById(produtoId, token)
                          ?? throw new ArgumentNullException("Produto não encontrado");
            
            if (fornecedor.Produtos.Any(p => p.Id == produto.Id))
            {
                throw new InvalidOperationException("Produto já associado a este fornecedor.");
            }

            fornecedor.Produtos.Add(produto);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedor);
        }
        public async Task<FornecedorResponse> DeleteProdutoFromFornecedorAsync
            (
               Guid fornecedorId,
               Guid produtoId,
               CancellationToken token = default
            )

        {
            var fornecedor = await _repo.GetByIdWithProdutosAsync(fornecedorId, token)
                ?? throw new ArgumentNullException("Fornecedor não encontrado");

            var produto = fornecedor.Produtos.FirstOrDefault(p => p.Id == produtoId)
                ?? throw new InvalidOperationException("Produto não associado a este fornecedor");

            fornecedor.Produtos.Remove(produto);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedor);
        }
        public async Task<List<FornecedorResponse>> GetByIdWithProdutosAsync
            (
                IEnumerable<Guid> produtoIds,
                CancellationToken token = default
            )
        {

            if (produtoIds == null || !produtoIds.Any())
            {
                throw new ArgumentException("Nenhum ID de produto fornecido.");
            }

            var produtosEncontrados = await _produtoRepo.GetByIdsAsync(produtoIds, token);


            if (produtosEncontrados == null || produtosEncontrados.Count == 0)
            {
                throw new ArgumentNullException("Nenhum produto encontrado para os IDs informados.");
            }

            var fornecedores = produtosEncontrados
            .Where(p => p.Fornecedores != null)
                .SelectMany(p => p.Fornecedores!)
                .Distinct()
                .ToList();

            if (fornecedores.Count == 0) { 
                throw new ArgumentNullException("Nenhum fornecedor associado encontrado para os produtos informados.");
            }

            return MapListToResponse(fornecedores);
        }

        private static FornecedorResponse MapToResponse(Fornecedor f) => 
            new FornecedorResponse
        {
                Id = f.Id,
                Nome = f.Nome,
                CreatedAt = f.CreatedAt,
                Produtos = f.Produtos?.Select(x => new ProdutoResponse
                {
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    LocalDescarga = x.LocalDescarga.Nome,
                    LocalDescargaId = x.LocalDescargaId,
                    Nome = x.Nome
                }).ToList()
        };

        private static List<FornecedorResponse> MapListToResponse
            (
                IEnumerable<Fornecedor> fornecedores
            ) => fornecedores.Select(MapToResponse).ToList();
    }
}

