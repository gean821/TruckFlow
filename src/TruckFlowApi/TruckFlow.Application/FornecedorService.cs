using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
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
        private readonly CurrentUserGuard _currentUser;

        public FornecedorService(
            IFornecedorRepositorio repo,
            IValidator<FornecedorCreateDto> createValidator,
            IValidator<FornecedorUpdateDto> updateValidator,
            IProdutoRepositorio produtoRepo,
            FornecedorFactory factory,
            CurrentUserGuard currentUser)
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _produtoRepo = produtoRepo;
            _factory = factory;
            _currentUser = currentUser;
        }

        public async Task<FornecedorResponse> CreateFornecedor(
            FornecedorCreateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var empresaId = _currentUser.GetEmpresaId();

            var cnpjLimpo = new string(dto.Cnpj.Where(char.IsDigit).ToArray());

            var existente = await _repo.GetByCnpj(cnpjLimpo, token);

            if (existente != null)
            {
                throw new BusinessException($"Já existe fornecedor com o CNPJ {dto.Cnpj}");
            }

            List<Produto> produtos = [];

            if (dto.ProdutoIds?.Count == 0)
            {
                produtos = await _produtoRepo.GetByIdsAsync(dto.ProdutoIds, token);
            }

            dto.Cnpj = cnpjLimpo;

            var fornecedor = await _factory.CreateFornecedorFromDto(dto, empresaId, produtos);

            await _repo.CreateFornecedor(fornecedor, token);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedor);
        }

        public async Task<List<FornecedorResponse>> GetAll(CancellationToken token = default)
        {
            var lista = await _repo.GetAll(token);
            return lista.Select(MapToResponse).ToList();
        }

        public async Task<FornecedorResponse> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            var fornecedor = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            return MapToResponse(fornecedor);
        }

        public async Task<FornecedorResponse> GetByCnpj(
            string cnpj,
            CancellationToken token = default
            ) 
        {
            var fornecedorEncontrado = await _repo.GetByCnpj(cnpj, token) 
                ?? throw new NotFoundException("Fornecedor não encontrado");
            
            return MapToResponse(fornecedorEncontrado);
        }


        public async Task<FornecedorResponse> UpdateFornecedor(
            Guid id,
            FornecedorUpdateDto dto,
            CancellationToken token = default)
        {
            var validation = await _updateValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var fornecedor = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            ApplyPatch(fornecedor, dto);

            await _repo.Update(fornecedor, token);

            return MapToResponse(fornecedor);
        }

        public async Task DeleteFornecedor(
            Guid id,
            CancellationToken token = default
            )
        {
            var fornecedor = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            await _repo.Delete(fornecedor, token);
        }

        public async Task<FornecedorResponse> AddProdutoToFornecedorAsync(
            Guid fornecedorId,
            Guid produtoId,
            CancellationToken token = default
            )
        {
            var fornecedor = await _repo.GetByIdWithProdutosAsync(fornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            var produto = await _produtoRepo.GetById(produtoId, token) 
                ?? throw new NotFoundException("Produto não encontrado");

            if (fornecedor.Produtos.Any(p => p.Id == produto.Id))
            {
                throw new BusinessException("Produto já associado a este fornecedor.");
            }

            fornecedor.Produtos.Add(produto);

            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedor);
        }

        public async Task<FornecedorResponse> DeleteProdutoFromFornecedorAsync(
            Guid fornecedorId,
            Guid produtoId,
            CancellationToken token = default
            )
        {
            var fornecedor = await _repo.GetByIdWithProdutosAsync(fornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            var produto = fornecedor.Produtos
                .FirstOrDefault(p => p.Id == produtoId)
                ?? throw new NotFoundException("Produto não associado");

            fornecedor.Produtos.Remove(produto);

            await _repo.SaveChangesAsync(token);

            return MapToResponse(fornecedor);
        }

        private static void ApplyPatch(Fornecedor fornecedor, FornecedorUpdateDto dto)
        {
            if (dto.Nome is not null)
                fornecedor.Nome = dto.Nome;

            if (dto.Cnpj is not null)
            {
                var cnpjLimpo = new string(dto.Cnpj.Where(char.IsDigit).ToArray());
                fornecedor.Cnpj = cnpjLimpo;
            }

            fornecedor.UpdatedAt = DateTime.UtcNow;
        }

        private static FornecedorResponse MapToResponse(Fornecedor f) =>
            new FornecedorResponse
            {
                Id = f.Id,
                Nome = f.Nome,
                Cnpj = f.Cnpj,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                Produtos = f.Produtos?.Select(p => new ProdutoResponse
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    LocalDescarga = p.LocalDescarga.Nome,
                    LocalDescargaId = p.LocalDescargaId,
                    CreatedAt = p.CreatedAt
                }).ToList()
            };
    }
}

