using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class EmpresaService : IEmpresaService
    {
        private readonly IEmpresaRepositorio _repo;
        private readonly IValidator<EmpresaCreateDto> _createValidator;
        private readonly IValidator<EmpresaUpdateDto> _updateValidator;

        public EmpresaService(
            IEmpresaRepositorio repo,
            IValidator<EmpresaCreateDto> createValidator,
            IValidator<EmpresaUpdateDto> updateValidator)
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<EmpresaResponseDto> CreateEmpresa(
            EmpresaCreateDto dto,
            CancellationToken token = default
            )
        {
            await _createValidator.ValidateAndThrowAsync(dto, token);

            var existing = await _repo.GetByCnpj(dto.Cnpj, token);

            if (existing is not null) { 
                throw new BusinessException("CNPJ já cadastrado.");
            }

            var empresa = EmpresaFactory.Create(dto);

            await _repo.CreateEmpresa(empresa, token);

            return MapToResponse(empresa);
        }

        public async Task<EmpresaResponseDto> GetById(
            Guid id,
            CancellationToken token = default)
        {
            var empresa = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            return MapToResponse(empresa);
        }

        public async Task<List<EmpresaResponseDto>> GetAll(
            CancellationToken token = default)
        {
            var empresas = await _repo.GetAll(token);

            return empresas
                .Select(MapToResponse)
                .ToList();
        }

        public async Task<EmpresaResponseDto> Update(
            Guid id,
            EmpresaUpdateDto dto,
            CancellationToken token = default
            )
        {
            await _updateValidator.ValidateAndThrowAsync(dto, token);

            var empresa = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            ApplyPatch(empresa, dto);

            empresa.UpdatedAt = DateTime.UtcNow;

            await _repo.Update(empresa, token);

            return MapToResponse(empresa);
        }

        public async Task Desativar(
            Guid id,
            CancellationToken token = default)
        {
            var empresa = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            empresa.Ativa = false;
            empresa.DeletedAt = DateTime.UtcNow;

            await _repo.Update(empresa, token);
        }

        private static void ApplyPatch(Empresa empresa, EmpresaUpdateDto dto)
        {
            if (dto.RazaoSocial != null)
            {
                empresa.RazaoSocial = dto.RazaoSocial;
            }

            if (dto.NomeFantasia != null)
            {
                empresa.NomeFantasia = dto.NomeFantasia;
            }

            if (dto.Email != null)
            {
                empresa.Email = dto.Email;
            }

            if (dto.Telefone != null)
            {
                empresa.Telefone = dto.Telefone;
            }

            if (dto.Cep != null)
            {
                empresa.Cep = dto.Cep;
            }

            if (dto.Logradouro != null)
            {
                empresa.Logradouro = dto.Logradouro;
            }

            if (dto.Numero != null)
            {
                empresa.Numero = dto.Numero;
            }

            if (dto.Complemento != null)
            {
                empresa.Complemento = dto.Complemento;
            }

            if (dto.Bairro != null)
            {
                empresa.Bairro = dto.Bairro;
            }

            if (dto.Cidade != null)
            {
                empresa.Cidade = dto.Cidade;
            }

            if (dto.Estado != null)
            {
                empresa.Estado = dto.Estado;
            }
        }

        private static EmpresaResponseDto MapToResponse(Empresa empresa)
        {
            return new EmpresaResponseDto
            {
                Id = empresa.Id,
                RazaoSocial = empresa.RazaoSocial,
                NomeFantasia = empresa.NomeFantasia,
                Cnpj = empresa.Cnpj,
                Email = empresa.Email,
                Cidade = empresa.Cidade,
                Estado = empresa.Estado,
                Ativa = empresa.Ativa,
                CreatedAt = empresa.CreatedAt
            };
        }
    }
}
