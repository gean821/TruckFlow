using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class VeiculoRepositorio : IVeiculoRepositorio
    {
        private readonly AppDbContext _db;

        public VeiculoRepositorio(AppDbContext db) => _db = db;

        public async Task<Veiculo> CreateVeiculo(Veiculo veiculo, CancellationToken token = default)
        {
            await _db.Veiculo.AddAsync(veiculo, token);
            return veiculo;
        }

        public async Task<List<Veiculo>> GetAll(CancellationToken token = default)
        {
            return await _db.Veiculo
                .Include(x => x.TipoVeiculo)
                .Include(x => x.Motorista)
                .ToListAsync(token);
        }

        public async Task<Veiculo> GetById(Guid id, CancellationToken token = default)
        {
            var veiculo =  await _db.Veiculo
                .Include(x => x.TipoVeiculo)
                .Include(x => x.Motorista)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return veiculo!;
        }

        public async Task<Veiculo> Update(Guid id, Veiculo veiculo, CancellationToken token = default)
        {
            var veiculoEncontrado = await GetById(id, token);

            veiculoEncontrado.Nome = veiculo.Nome;
            veiculoEncontrado.Placa = veiculo.Placa;
            veiculoEncontrado.TipoVeiculo = veiculo.TipoVeiculo;
            veiculoEncontrado.Motorista = veiculo.Motorista;
            veiculoEncontrado.MotoristaId = veiculo.MotoristaId;

            await SaveChangesAsync(token);
            return veiculoEncontrado;
        }

        public async Task Delete(Guid id, CancellationToken token = default)
        {
            var veiculoEncontrado = GetById(id, token);

            _db.Remove(veiculoEncontrado);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
