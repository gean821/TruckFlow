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
    public class MotoristaRepositorio : IMotoristaRepositorio
    {
        private readonly AppDbContext _db;

        public MotoristaRepositorio(AppDbContext db) => _db = db;
    
        public async Task<Motorista> CreateMotorista(Motorista motorista, CancellationToken token = default)
        {
            await _db.Motorista.AddAsync(motorista, token);
            return motorista;
        }

        public async Task<List<Motorista>> GetAll(CancellationToken token = default)
        {
            return await _db.Motorista
                .Include(x => x.Nome)
                .Include(x => x.Telefone)
                .Include(x => x.Veiculo)
                .ToListAsync(token);
        }


        public async Task<Motorista> GetById(Guid id, CancellationToken token = default)
        {
            var motorista = await _db.Motorista
                .Include(x => x.Nome)
                .Include(x => x.Telefone)
                .Include(x => x.Veiculo)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return motorista!;
        }

        public async Task<Motorista> UpdateMotorista(Guid id, Motorista motorista, CancellationToken token = default)
        {
            var motoristaEncontrado = await GetById(id, token);

            motoristaEncontrado.Id = motorista.Id;
            motoristaEncontrado.Nome = motorista.Nome;
            motoristaEncontrado.Telefone = motorista.Telefone;
            motoristaEncontrado.Veiculo = motorista.Veiculo;
            motoristaEncontrado.VeiculoId = motorista.VeiculoId;


            await SaveChangesAsync(token);
            return motoristaEncontrado;
        }

        public async Task Delete(Guid id, CancellationToken token = default)
        {
            var motoristaEncontrado = await GetById(id, token);

            _db.Remove(motoristaEncontrado);
            await SaveChangesAsync(token);
        }

        public Task SaveChangesAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
