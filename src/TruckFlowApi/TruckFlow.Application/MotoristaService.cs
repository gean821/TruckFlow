using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class MotoristaService : IMotoristaService
    {
        private readonly IMotoristaRepositorio _repo;

        public MotoristaService(IMotoristaRepositorio repo)
        {
            _repo = repo;
        }

        public async Task<UserMotoristaResponseDto> GetMe
            (   
                Guid usuarioId,
                CancellationToken token = default
            )
        {
            var motorista = await _repo.GetByUsuarioId(usuarioId, token)
                ?? throw new NotFoundException("Usuario não encontrado.");

            return MapToResponse(motorista);
        }

        public async Task<List<VeiculoResponse>> GetMeusVeiculos
            (
                Guid usuarioId,
                CancellationToken token = default
            )
        {
            var veiculos = await _repo.GetMeusVeiculos(usuarioId, token);
            return veiculos.Select(x => new VeiculoResponse
            {
                Id = x.Id,
                Placa = x.Placa,
                TipoVeiculo = x.TipoVeiculo,
                UpdatedAt = x.UpdatedAt,
                CreatedAt = x.CreatedAt
            }).ToList();
           
        }

        private UserMotoristaResponseDto MapToResponse(Motorista motorista)
        {
            return new UserMotoristaResponseDto
            {
                Id = motorista.Id,
                Username = motorista.Nome,
                Email = motorista.Usuario.Email!,
                Telefone = motorista.Telefone,
                DeletedAt = motorista.DeletedAt,
                UpdatedAt = motorista.UpdatedAt,
                CreatedAt = motorista.CreatedAt,
            };
        }
    }
}

        