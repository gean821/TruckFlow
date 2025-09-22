using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Application.Dto.Fornecedor;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
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

        public async Task<FornecedorResponse> CreateFornecedor(
            FornecedorCreateDto fornecedor,
            CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(fornecedor, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var fornecedorCriado = await _factory.CreateFornecedorFromDto(fornecedor, cancellationToken);

            await _repo.CreateFornecedor(fornecedorCriado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new FornecedorResponse
            {
                Id = fornecedorCriado.Id,
                Nome = fornecedorCriado.Nome,
            };
        }

        public async Task<FornecedorResponse> GetById(Guid id, CancellationToken cancellatioToken = default)
        {
            var fornecedorEncontrado = await _repo.GetById(id, cancellatioToken)
                ?? throw new ArgumentNullException("Fornecedor não encontrado");

            return new FornecedorResponse
            {
                Id = fornecedorEncontrado.Id,
                Nome = fornecedorEncontrado.Nome,
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

            var listaFornecedorDto = listaFornecedor.Select(x => new FornecedorResponse
            {
                Id = x.Id,
                Nome = x.Nome,
            }).ToList();
            
            return listaFornecedorDto;
        }

        public async Task<FornecedorResponse> UpdateFornecedor(
            Guid id,
            FornecedorUpdateDto fornecedor,
            CancellationToken cancellationToken = default)
        {
            var produto = await _produtoRepo.GetById(fornecedor.ProdutoId, cancellationToken)
                ?? throw new ArgumentNullException("Produto associado não encontrado.");

            ValidationResult validationResult = await _updateValidator.ValidateAsync(fornecedor, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var fornecedorEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Fornecedor não encontrado.");

            fornecedorEncontrado.Nome = fornecedor.Nome;
            //fornecedorEncontrado.Produto = produto;
            //fornecedorEncontrado.ProdutoId = produto.Id;

            var fornecedorAtualizado = await _repo.Update(id, fornecedorEncontrado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new FornecedorResponse
            {
                Id = fornecedorAtualizado.Id,
                Nome = fornecedorAtualizado.Nome,
                //Produto = fornecedorAtualizado.Produto.Nome
            };
        }

        private FornecedorResponse MapToResponse(Fornecedor f) => 
            new FornecedorResponse
        {
                Id = f.Id,
                Nome = f.Nome,
                //Produto = f.Produto.Nome,
                CreatedAt = f.CreatedAt
        };
    }
}

