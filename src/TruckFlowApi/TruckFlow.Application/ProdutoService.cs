using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlow.Application.Exceptions;

namespace TruckFlow.Application
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepositorio _repo;
        private readonly IValidator<ProdutoCreateDto> _createValidator;
        private readonly IValidator<ProdutoEditDto> _editValidator;
        private readonly ProdutoFactory _factory;
        private readonly ILocalDescargaRepositorio _localRepo;
        private readonly CurrentUserGuard _currentUser;

        public ProdutoService(
            IProdutoRepositorio repo,
            IValidator<ProdutoCreateDto> createValidator,
            IValidator<ProdutoEditDto> editValidator,
            ProdutoFactory factory,
            ILocalDescargaRepositorio localRepo,
            CurrentUserGuard currentUser)
        {
            _repo = repo;
            _createValidator = createValidator;
            _editValidator = editValidator;
            _factory = factory;
            _localRepo = localRepo;
            _currentUser = currentUser;
        }

        public async Task<ProdutoResponse> CreateProduto(
            ProdutoCreateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var empresaId = _currentUser.GetEmpresaId();

            var entity = await _factory.CreateProdutoFromDto(dto, empresaId, token);

            await _repo.CreateProduto(entity, token);

            return MapToResponse(entity);
        }

        public async Task<List<ProdutoResponse>> GetAll(CancellationToken token = default)
        {
            var lista = await _repo.GetAll(token);

            return lista.Select(MapToResponse).ToList();
        }

        public async Task<ProdutoResponse> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            var produto = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Produto não encontrado");

            return MapToResponse(produto);
        }

        public async Task DeleteProduto(
            Guid id,
            CancellationToken token = default
            )
        {
            var produto = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Produto não encontrado");

            await _repo.DeleteProduto(produto, token);
        }

        public async Task<ProdutoResponse> UpdateProduto(
            Guid id,
            ProdutoEditDto dto,
            CancellationToken token = default)
        {
            var validation = await _editValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var produto = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Produto não encontrado");

            await ApplyPatch(produto, dto, token);

            await _repo.UpdateProduto(produto, token);

            return MapToResponse(produto);
        }

        private async Task ApplyPatch(
            Produto produto,
            ProdutoEditDto dto,
            CancellationToken token
            )
        {
            if (dto.Nome is not null)
                produto.Nome = dto.Nome;

            if (dto.LocalDescargaId is not null)
            {
                var localDescarga = await _localRepo.GetById(dto.LocalDescargaId.Value, token)
                    ?? throw new NotFoundException("Local de descarga não encontrado");

                produto.LocalDescarga = localDescarga;
                produto.LocalDescargaId = localDescarga.Id;
            }

            produto.UpdatedAt = DateTime.UtcNow;
        }

        private static ProdutoResponse MapToResponse(Produto p) =>
            new ProdutoResponse
            {
                Id = p.Id,
                Nome = p.Nome,
                LocalDescarga = p.LocalDescarga.Nome,
                LocalDescargaId = p.LocalDescargaId,
                CodigoEan = p.CodigoEan,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
    }
}
