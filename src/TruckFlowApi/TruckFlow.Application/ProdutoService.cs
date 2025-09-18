using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Produto;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepositorio _repo;
        private readonly IValidator<ProdutoCreateDto> _createValidator;
        private readonly IValidator<ProdutoEditDto> _editValidator;
        private readonly ILocalDescargaRepositorio _localRepo;
        private readonly ProdutoFactory _factory;

        public ProdutoService(
            IProdutoRepositorio repo,
            IValidator<ProdutoCreateDto> createValidator,
            IValidator<ProdutoEditDto> editValidator,
            ProdutoFactory factory,
            ILocalDescargaRepositorio localRepo
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _editValidator = editValidator;
            _factory = factory;
            _localRepo = localRepo;
        }

        public async Task<ProdutoResponse> CreateProduto(
            ProdutoCreateDto produto,
            CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(produto, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var produtoCriado = await _factory.CreateProdutoFromDto(produto, cancellationToken);

            await _repo.CreateProduto(produtoCriado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new ProdutoResponse
            {
                Id = produtoCriado.Id,
                Nome = produtoCriado.Nome,
                LocalDescarga = produtoCriado.LocalDescarga.Nome,
                CreatedAt = produtoCriado.CreatedAt
            };
        }

        public async Task<ProdutoResponse> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var produtoEncontrado = await _repo.GetById(id, cancellationToken) ?? 
                throw new ArgumentNullException("Produto não encontrado"); //aqui vai ser implementado com Resources futuramente para mensagens do sistema.

            return new ProdutoResponse
            {
                Id = produtoEncontrado.Id,
                Nome = produtoEncontrado.Nome,
                LocalDescarga = produtoEncontrado.LocalDescarga.Nome,
                CreatedAt = produtoEncontrado.CreatedAt
            };
        }

        public async Task DeleteProduto(Guid id, CancellationToken cancellationToken = default)
        {
            var produtoEncontrado = await _repo.GetById(id, cancellationToken) 
                ?? throw new ArgumentNullException("Produto não encontrado");

            await _repo.DeleteProduto(produtoEncontrado.Id, cancellationToken);
            
            await _repo.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<ProdutoResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listaProdutos = await _repo.GetAll(cancellationToken);
            
            if (listaProdutos.Count == 0)
            {
                return new List<ProdutoResponse>();
            }

            var listaProdutosDto = listaProdutos.Select(x => new ProdutoResponse
            {
                Id = x.Id,
                Nome = x.Nome,
                LocalDescarga = x.LocalDescarga.Nome,
                CreatedAt = x.CreatedAt
            }).ToList();

            return listaProdutosDto;
        }


        public async Task<ProdutoResponse> UpdateProduto(
            Guid id,
            ProdutoEditDto produto,
            CancellationToken cancellationToken = default)
        {
            var localDescarga = await _localRepo.GetById(produto.LocalDescargaId, cancellationToken) 
                ?? throw new ArgumentNullException("Local de descarga não encontrado");

            ValidationResult validationResult = await _editValidator.ValidateAsync(produto, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var produtoEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Produto não encontrado");


            produtoEncontrado.Nome = produto.Nome;
            produtoEncontrado.LocalDescarga = localDescarga;
            produtoEncontrado.LocalDescargaId = localDescarga.Id;
            produtoEncontrado.UpdatedAt = DateTime.UtcNow;

            var produtoAtualizado = await _repo.UpdateProduto(id, produtoEncontrado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new ProdutoResponse
            {
                Id = produtoAtualizado.Id,
                Nome = produtoAtualizado.Nome,
                LocalDescarga = produtoAtualizado.LocalDescarga.Nome,
                CreatedAt = produtoAtualizado.CreatedAt,
                UpdatedAt = produtoAtualizado.UpdatedAt
            };
        }

        private ProdutoResponse MapToResponse(Produto p) => 

            new ProdutoResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                LocalDescarga = p.LocalDescarga.Nome,
                CreatedAt = p.CreatedAt
            };
       }
}
