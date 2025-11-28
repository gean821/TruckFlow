using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class VeiculoFactory
    {
        private readonly IMotoristaRepositorio _repo;

        public VeiculoFactory(IMotoristaRepositorio repo) => _repo = repo;

        public async Task<Veiculo> CreateVeiculoFromDto(VeiculoCreateDto dto, CancellationToken token = default)
        {
            var motorista = await _repo.GetById(dto.MotoristaId, token)
                ?? throw new ArgumentNullException("Não foi possível encontrar o motorista");

            return new Veiculo
            {
                Nome = dto.Nome,
                Placa = dto.Placa,
                TipoVeiculo = dto.TipoVeiculo,
                Motorista = motorista,
                MotoristaId = motorista.Id
            };
        }
    }
}
