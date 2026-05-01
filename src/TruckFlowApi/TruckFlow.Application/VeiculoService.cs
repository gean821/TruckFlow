using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlow.Domain.Entities;
using TruckFlow.Application.Exceptions;

namespace TruckFlow.Application
{
    public class VeiculoService : IVeiculoService
    {
        private readonly IVeiculoRepositorio _repo;
        private readonly IValidator<VeiculoCreateDto> _createValidator;
        private readonly IValidator<VeiculoUpdateDto> _updateValidator;
        private readonly IMotoristaRepositorio _motoristaRepo;
        private readonly VeiculoFactory _factory;
        private readonly ILogger<VeiculoService> _logger;

        public VeiculoService(
            IVeiculoRepositorio repo,
            IValidator<VeiculoCreateDto> createValidator,
            IValidator<VeiculoUpdateDto> updateValidator,
            VeiculoFactory factory,
            IMotoristaRepositorio motoristaRepo,
            ILogger<VeiculoService> logger
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _motoristaRepo = motoristaRepo;
            _logger = logger;
        }

        public async Task<VeiculoResponse> CreateVeiculo(
            VeiculoCreateDto veiculo, 
            CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(veiculo, cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao criar veículo: {Errors}",
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validationResult.Errors);
            }

            var veiculoCriado = await _factory.CreateVeiculoFromDto(veiculo, cancellationToken);

            await _repo.CreateVeiculo(veiculoCriado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Veículo criado: {VeiculoId} placa {Placa}",
                veiculoCriado.Id, veiculoCriado.Placa);

            return MapToResponse(veiculoCriado);
        }

        public async Task<VeiculoResponse> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var veiculoEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Veículo não encontrado.");

            return MapToResponse(veiculoEncontrado);
        }

        public async Task DeleteVeiculo(Guid id, CancellationToken cancellationToken = default)
        {
            var veiculoEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Veículo não encontrado.");

            await _repo.Delete(veiculoEncontrado.Id, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Veículo excluído: {VeiculoId}",
                id);
        }

        public async Task<List<VeiculoResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listaVeiculo = await _repo.GetAll(cancellationToken);

            if (listaVeiculo.Count == 0)
            {
                return new List<VeiculoResponse>();
            }

            var listaVeiculoDto = listaVeiculo.Select(v => MapToResponse(v)).ToList();
                
            return listaVeiculoDto;
        }

        public async Task<VeiculoResponse> UpdateVeiculo(
            Guid id,
            VeiculoUpdateDto veiculo,
            CancellationToken cancellationToken = default)
        {
            var motorista = await _motoristaRepo.GetByUsuarioId(veiculo.MotoristaId, cancellationToken)
                ?? throw new NotFoundException("Motorista não encontrado.");

            ValidationResult validationResult = await _updateValidator.ValidateAsync(veiculo, cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao atualizar veículo {VeiculoId}: {Errors}",
                    id, string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validationResult.Errors);
            }

            var veiculoEncontrado = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Veículo não encontrado.");

            veiculoEncontrado.Placa = veiculo.Placa;
            veiculoEncontrado.TipoVeiculo = veiculo.TipoVeiculo;
            veiculoEncontrado.Motorista = motorista;
            veiculoEncontrado.MotoristaId = motorista.Id;

            var veiculoAtualizado = await _repo.Update(id, veiculoEncontrado, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Veículo atualizado: {VeiculoId} placa {Placa}",
                id, veiculoAtualizado.Placa);

            return MapToResponse(veiculoAtualizado);
        }

        private static VeiculoResponse MapToResponse(Veiculo v) =>
            new VeiculoResponse
            {
                Id = v.Id,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                Motorista = v.Motorista.NomeReal,
                CreatedAt = v.CreatedAt
            };
    }
}
