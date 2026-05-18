using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.UnidadeEntrega;
using TruckFlow.Domain.Dto.UnidadeEntrega;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class UnidadeEntregaService : IUnidadeEntregaService
    {
        private readonly IUnidadeEntregaRepositorio _repo;
        private readonly IValidator<UnidadeEntregaCreateDto> _createValidator;
        private readonly IValidator<UnidadeEntregaUpdateDto> _updateValidator;
        private readonly CurrentUserGuard _currentUser;
        private readonly IGeocodingService _geocoding;
        private readonly ILogger<UnidadeEntregaService> _logger;

        public UnidadeEntregaService(
            IUnidadeEntregaRepositorio repo,
            IValidator<UnidadeEntregaCreateDto> createValidator,
            IValidator<UnidadeEntregaUpdateDto> updateValidator,
            CurrentUserGuard currentUser,
            IGeocodingService geocoding,
            ILogger<UnidadeEntregaService> logger)
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _currentUser = currentUser;
            _geocoding = geocoding;
            _logger = logger;
        }

        public async Task<UnidadeEntregaResponse> CreateUnidadeEntrega(
            UnidadeEntregaCreateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao criar unidade de entrega: {Errors}",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var empresaId = _currentUser.GetEmpresaId();

            var entity = new UnidadeEntrega
            {
                Nome = dto.Nome,
                Localizacao = dto.Localizacao,
                Logradouro = dto.Logradouro,
                Numero = dto.Numero,
                Complemento = dto.Complemento,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                Cep = dto.Cep,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                EmpresaId = empresaId,
                CreatedAt = DateTime.UtcNow
            };

            if (entity.Latitude is null && entity.Longitude is null)
            {
                var geo = await _geocoding.GeocodeAsync(entity, token);
                
                if (geo is not null)
                {
                    entity.Latitude = geo.Latitude;
                    entity.Longitude = geo.Longitude;
                }
            }

            await _repo.CreateUnidadeEntrega(entity, token);
            await _repo.SaveChangesAsync(token);

            _logger.LogInformation(
                "Unidade de entrega criada: {UnidadeId} {Nome} (empresa {EmpresaId})",
                entity.Id, entity.Nome, empresaId);

            return MapToResponse(entity);
        }

        public async Task<List<UnidadeEntregaResponse>> GetAll(CancellationToken token = default)
        {
            var lista = await _repo.GetAll(token);

            return lista.Select(MapToResponse).ToList();
        }

        public async Task<UnidadeEntregaResponse> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            var unidade = await _repo.GetById(id,token)
                ?? throw new NotFoundException("Unidade de entrega não encontrada");

            return MapToResponse(unidade);
        }

        public async Task DeleteUnidadeEntrega(
            Guid id,
            CancellationToken token = default
            )
        {
            var empresaId = _currentUser.GetEmpresaId();

            var unidade = await _repo.GetById(id,token)
                ?? throw new NotFoundException("Unidade de entrega não encontrada");

            await _repo.Delete(unidade, token);

            _logger.LogInformation(
                "Unidade de entrega excluída: {UnidadeId} (empresa {EmpresaId})",
                id, empresaId);
        }

        public async Task<UnidadeEntregaResponse> UpdateUnidadeEntrega(
            Guid id,
            UnidadeEntregaUpdateDto dto,
            CancellationToken token = default
            )
        {
            var empresaId = _currentUser.GetEmpresaId();

            var validation = await _updateValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao atualizar unidade de entrega {UnidadeId}: {Errors}",
                    id, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var unidade = await _repo.GetById(id,token)
                ?? throw new NotFoundException("Unidade de entrega não encontrada");

            ApplyPatch(unidade, dto);

            var enderecoMudou = dto.Logradouro is not null
                || dto.Numero is not null
                || dto.Bairro is not null
                || dto.Cidade is not null
                || dto.Estado is not null
                || dto.Cep is not null;

            if (enderecoMudou && dto.Latitude is null && dto.Longitude is null)
            {
                var geo = await _geocoding.GeocodeAsync(unidade, token);
                if (geo is not null)
                {
                    unidade.Latitude = geo.Latitude;
                    unidade.Longitude = geo.Longitude;
                }
            }

            await _repo.Update(unidade, token);

            _logger.LogInformation(
                "Unidade de entrega atualizada: {UnidadeId} (empresa {EmpresaId})",
                id, empresaId);

            return MapToResponse(unidade);
        }

        public async Task<UnidadeEntregaResponse> MudarStatusUnidade(
            Guid id,
            bool status,
            CancellationToken token = default
            )
        {
            var unidade = await _repo.GetById(id, token)
                  ?? throw new NotFoundException("Unidade não encontrada.");

            unidade.Ativa = status;
            unidade.UpdatedAt = DateTime.UtcNow;

            await _repo.Update(unidade, token);

            _logger.LogInformation(
                "Unidade de entrega {UnidadeId} mudou status para {Ativa}",
                id, status);

            return MapToResponse(unidade);
        }

        private static void ApplyPatch(
            UnidadeEntrega unidade,
            UnidadeEntregaUpdateDto dto
            )
        {
            if (dto.Nome is not null)
                unidade.Nome = dto.Nome;

            if (dto.Localizacao is not null)
                unidade.Localizacao = dto.Localizacao;

            if (dto.Logradouro is not null)
                unidade.Logradouro = dto.Logradouro;

            if (dto.Numero is not null)
                unidade.Numero = dto.Numero;

            if (dto.Complemento is not null)
                unidade.Complemento = dto.Complemento;

            if (dto.Bairro is not null)
                unidade.Bairro = dto.Bairro;

            if (dto.Cidade is not null)
                unidade.Cidade = dto.Cidade;

            if (dto.Estado is not null)
                unidade.Estado = dto.Estado;

            if (dto.Cep is not null)
                unidade.Cep = dto.Cep;

            if (dto.Latitude is not null)
                unidade.Latitude = dto.Latitude;

            if (dto.Longitude is not null)
                unidade.Longitude = dto.Longitude;

            if (dto.Ativa is not null)
            {
                unidade.Ativa = dto.Ativa;
            }

            unidade.UpdatedAt = DateTime.UtcNow;
        }
        private static UnidadeEntregaResponse MapToResponse(UnidadeEntrega unidade)
        {
            return new UnidadeEntregaResponse
            {
                Id = unidade.Id,
                Nome = unidade.Nome,
                Localizacao = unidade.Localizacao,
                Logradouro = unidade.Logradouro,
                Numero = unidade.Numero,
                Complemento = unidade.Complemento,
                Bairro = unidade.Bairro,
                Cidade = unidade.Cidade,
                Estado = unidade.Estado,
                Cep = unidade.Cep,
                Latitude = unidade.Latitude,
                Longitude = unidade.Longitude,
                Empresa = unidade?.Empresa?.NomeFantasia,
                Ativa = unidade?.Ativa
            };
        }
    }
}
