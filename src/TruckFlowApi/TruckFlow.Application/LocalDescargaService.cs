using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.LocalDescarga;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlow.Application.Exceptions;

namespace TruckFlow.Application
{
    public class LocalDescargaService : ILocalDescargaService
    {
        private readonly ILocalDescargaRepositorio _repo;
        private readonly IValidator<LocalDescargaCreateDto> _createValidator;
        private readonly IValidator<LocalDescargaUpdateDto> _updateValidator;
        private readonly IUnidadeEntregaRepositorio _unidadeEntregaRepositorio;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<LocalDescargaService> _logger;

        public LocalDescargaService(
            ILocalDescargaRepositorio repo,
            IValidator<LocalDescargaCreateDto> createValidator,
            IValidator<LocalDescargaUpdateDto> updateValidator,
            IUnidadeEntregaRepositorio unidadeEntregaRepositorio,
            ICurrentUserService currentUser,
            ILogger<LocalDescargaService> logger
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _unidadeEntregaRepositorio = unidadeEntregaRepositorio;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<LocalDescargaResponse> CreateLocalDescarga(
            LocalDescargaCreateDto local,
            CancellationToken token = default
            )
        {

            ValidationResult validation = await _createValidator.ValidateAsync(local, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao criar local de descarga: {Errors}",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var empresaId = ValidateEnterprise();

            var unidade = await _unidadeEntregaRepositorio.GetById(local.UnidadeEntregaId, token)
                ?? throw new NotFoundException("Unidade de entrega não encontrada.");

            var entity = new LocalDescarga
            {
                Nome = local.Nome,
                UnidadeEntrega = unidade,
                CreatedAt = DateTime.UtcNow,
                EmpresaId = empresaId,
                Ativa = local.Status
            };

            await _repo.CreateLocalDescarga(entity, token);
            await _repo.SaveChangesAsync(token);

            _logger.LogInformation(
                "Local de descarga criado: {LocalId} {Nome} (empresa {EmpresaId}, ativa {Ativa})",
                entity.Id, entity.Nome, empresaId, entity.Ativa);

            return MapToResponse(entity);
        }
        public async Task<List<LocalDescargaResponse>> GetAll(LocalDescargaListQueryDto query, CancellationToken token = default)
        {
            var listaDescarga = await _repo.GetAll(query, token);

            var listaDto = listaDescarga.Select(x => new LocalDescargaResponse
            {
                Id = x.Id,
                Nome = x.Nome,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                UnidadeEntrega = x.UnidadeEntrega.Localizacao,
                Status = x.Ativa,
                Produtos = x.Produtos?.Select(x => new ProdutoResponse
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    LocalDescarga = x.LocalDescarga.Nome,
                    LocalDescargaId = x.LocalDescargaId,
                    CreatedAt = x.CreatedAt
                }).ToList()
            }).ToList();

            return listaDto;
        }

        public async Task Delete(
            Guid id,
            CancellationToken token = default
            )
        {
            var empresaId = ValidateEnterprise();
            var localEncontrado = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Local não encontrado");


            await _repo.Delete(localEncontrado, token);
            await _repo.SaveChangesAsync(token);

            _logger.LogInformation(
                "Local de descarga excluído: {LocalId} (empresa {EmpresaId})",
                id, empresaId);
        }

        public async Task<LocalDescargaResponse> GetById(
            Guid id,
            CancellationToken token = default)
        {

            ValidateEnterprise();
            var localEncontrado = await _repo.GetById(id, token)
               ?? throw new NotFoundException("Local não encontrado");

            return MapToResponse(localEncontrado);
        }

        public async Task<LocalDescargaResponse> Update(
            Guid id,
            LocalDescargaUpdateDto local,
            CancellationToken token = default
            )
        {
            var empresaId = ValidateEnterprise();
            ValidationResult validation = await _updateValidator.ValidateAsync(local, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao atualizar local de descarga {LocalId}: {Errors}",
                    id, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var localEncontrado = await _repo.GetById(id, token)
               ?? throw new NotFoundException("Local não encontrado");

            var unidade = await _unidadeEntregaRepositorio.GetById(local.UnidadeEntregaId, token)
                    ?? throw new NotFoundException("Unidade não encontrada");

            localEncontrado.Nome = local.Nome;
            localEncontrado.UnidadeEntrega.Id = unidade.Id;
            localEncontrado.UpdatedAt = DateTime.UtcNow;
            localEncontrado.Ativa = local.Status;

            var localDescargaAtualizado = await _repo.Update(
                localEncontrado,
                token
                );


            await _repo.SaveChangesAsync(token);

            _logger.LogInformation(
                "Local de descarga atualizado: {LocalId} (empresa {EmpresaId})",
                id, empresaId);

            return MapToResponse(localDescargaAtualizado);
        }

        public async Task<LocalDescargaResponse> MudarStatusLocal
            (
             Guid localId,
             bool ativa,
             CancellationToken token = default
            )
        {
            var local = await _repo.GetById(localId, token)
                ?? throw new NotFoundException("Local não encontrado.");

            local.Ativa = ativa;
            local.UpdatedAt = DateTime.UtcNow;

            await _repo.Update(local, token);

            _logger.LogInformation(
                "Local de descarga {LocalId} mudou status para {Ativa}",
                localId, ativa);

            return MapToResponse(local);
        }

        private static LocalDescargaResponse MapToResponse(LocalDescarga local)
        {
            return new LocalDescargaResponse
            {
                Id = local.Id,
                Nome = local.Nome,
                CreatedAt = local.CreatedAt,
                UpdatedAt = local.UpdatedAt,
                DeletedAt = local.DeletedAt,
                UnidadeEntrega = local.UnidadeEntrega.Localizacao,
                Status = local.Ativa,
                Produtos = local.Produtos?.Select(x => new ProdutoResponse
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    LocalDescarga = x.LocalDescarga.Nome,
                    LocalDescargaId = x.LocalDescargaId,
                    CreatedAt = x.CreatedAt
                }).ToList()
            };
        }

        private Guid ValidateEnterprise()
        {
            return _currentUser.EmpresaId
                          ?? throw new BusinessException("Usuário não vinculado a empresa.");
        }
    }
}

     