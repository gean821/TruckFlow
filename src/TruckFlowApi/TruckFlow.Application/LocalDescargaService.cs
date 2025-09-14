using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.LocalDescarga;
using TruckFlow.Application.Dto.Produto;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class LocalDescargaService : ILocalDescargaService
    {
        private readonly ILocalDescargaRepositorio _repo;
        private readonly IValidator<LocalDescargaCreateDto> _createValidator;
        private readonly IValidator<LocalDescargaUpdateDto> _updateValidator;

        public LocalDescargaService(
            ILocalDescargaRepositorio repo,
            IValidator<LocalDescargaCreateDto> createValidator,
            IValidator<LocalDescargaUpdateDto> updateValidator)
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<LocalDescargaResponse> CreateLocalDescarga(
            LocalDescargaCreateDto local,
            CancellationToken token = default)
        {
            ValidationResult validation = await _createValidator.ValidateAsync(local, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var entity = new LocalDescarga
            {
                Nome = local.Nome
            };

            await _repo.CreateLocalDescarga(entity, token);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(entity);
        }
        public async Task<List<LocalDescargaResponse>> GetAll(CancellationToken token = default)
        {
            var listaDescarga = await _repo.GetAll(token);

            if (listaDescarga.Count == 0)
            {
                return new List<LocalDescargaResponse>();
            }

            var listaDto = listaDescarga.Select(x => new LocalDescargaResponse
            {
                Id = x.Id,
                Nome = x.Nome,
                Produtos = x.Produtos?.Select(x => new ProdutoResponse
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    LocalDescarga = x.LocalDescarga.Nome
                }).ToList()
            }).ToList();

            return listaDto;
        }

        public async Task Delete(Guid id, CancellationToken token = default)
        {
            var localEncontrado = await _repo.GetById(id, token) 
                ?? throw new ArgumentNullException("Local não encontrado");

            await _repo.Delete(localEncontrado.Id, token);

            await _repo.SaveChangesAsync(token);
        }

        public async Task<LocalDescargaResponse> GetById(
            Guid id,
            CancellationToken token = default)
        {
            var localEncontrado = await _repo.GetById(id, token)
               ?? throw new ArgumentNullException("Local não encontrado");

            return MapToResponse(localEncontrado);
        }

        public async Task<LocalDescargaResponse> Update(
            Guid id,
            LocalDescargaUpdateDto local,
            CancellationToken token = default)
        {
            ValidationResult validation = await _updateValidator.ValidateAsync(local, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var localEncontrado = await _repo.GetById(id, token)
               ?? throw new ArgumentNullException("Local não encontrado");

            localEncontrado.Nome = local.Nome;

            var localDescargaAtualizado = await _repo.Update(id, localEncontrado, token);
            await _repo.SaveChangesAsync(token);

            return MapToResponse(localDescargaAtualizado);
        }


        private static LocalDescargaResponse MapToResponse(LocalDescarga local)
        {
            return new LocalDescargaResponse
            {
                Id = local.Id,
                Nome = local.Nome,
                Produtos = local.Produtos?.Select(x => new ProdutoResponse
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    LocalDescarga = x.LocalDescarga.Nome
                }).ToList()
            };
        }
    }
}

     