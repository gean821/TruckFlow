using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.UnidadeEntrega;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Domain.Dto.UnidadeEntrega;

namespace TruckFlow.Application
{
    public class UnidadeEntregaService : IUnidadeEntregaService
    {
        private readonly IUnidadeEntregaRepositorio _repo;
        private readonly IValidator<UnidadeEntregaCreateDto> _createValidator;
        private readonly IValidator<UnidadeEntregaUpdateDto> _updateValidator;
        private readonly UnidadeEntregaFactory _factory;

        public UnidadeEntregaService(
            IUnidadeEntregaRepositorio repo,
            IValidator<UnidadeEntregaCreateDto> createValidator,
            IValidator<UnidadeEntregaUpdateDto> updateValidator,
            UnidadeEntregaFactory factory
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
        }

        public async Task<UnidadeEntregaResponse> CreateUnidadeEntrega(
            UnidadeEntregaCreateDto unidade,
            CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(unidade, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var unidadeCriada =  _factory.CreateUnidadeEntregaFromDto(unidade);

            await _repo.CreateUnidadeEntrega(unidadeCriada, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new UnidadeEntregaResponse
            {
                Id = unidadeCriada.Id,
                Nome = unidadeCriada.Nome,
                Localizacao = unidadeCriada.Localizacao,
            };
        }

        public async Task<UnidadeEntregaResponse> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var unidadeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Unidade de entrega não encontrada");

            return new UnidadeEntregaResponse
            {
                Id = unidadeEncontrada.Id,
                Nome = unidadeEncontrada.Nome,
                Localizacao = unidadeEncontrada.Localizacao,
            };
        }

        public async Task DeleteUnidadeEntrega(Guid id, CancellationToken cancellationToken = default)
        {
            var unidadeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Unidade de entrega não encontrada");

            await _repo.Delete(unidadeEncontrada.Id, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<UnidadeEntregaResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listaUnidade = await _repo.GetAll(cancellationToken);

            if (listaUnidade.Count == 0)
            {
                return new List<UnidadeEntregaResponse>();
            }

            var listaUnidadeDto = listaUnidade.Select(x => new UnidadeEntregaResponse
            {
                Id = x.Id,
                Nome = x.Nome,
                Localizacao = x.Localizacao,
            }).ToList();

            return listaUnidadeDto;
        }

        public async Task<UnidadeEntregaResponse> UpdateUnidadeEntrega(
            Guid id, 
            UnidadeEntregaUpdateDto unidade,
            CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await _updateValidator.ValidateAsync(unidade, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var unidadeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new ArgumentNullException("Unidade de entrega não encontrada");

            unidadeEncontrada.Nome = unidade.Nome;
            unidadeEncontrada.Localizacao = unidade.Localizacao;

            var unidadeAtualizada = await _repo.Update(id, unidadeEncontrada, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return new UnidadeEntregaResponse
            {
                Id = unidadeEncontrada.Id,
                Nome = unidadeEncontrada.Nome,
                Localizacao = unidadeEncontrada.Localizacao,
            };
        }
    }
}
