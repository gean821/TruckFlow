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
    internal class PlanejamentoRecebimentoRepositorio : IPlanejamentoRecebimentoRepositorio
    {
        private readonly AppDbContext _db;

        public PlanejamentoRecebimentoRepositorio(AppDbContext db) => _db = db;
        
        public async Task<List<PlanejamentoRecebimento>> GetAll(CancellationToken token = default)
        {
            return await _db.PlanejamentosRecebimento
                .Include(x => x.Fornecedor)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(x => x.Produto)
                .ToListAsync(token);
        }
        public async Task<PlanejamentoRecebimento> CreateRecebimento
            (
                PlanejamentoRecebimento recebimento,
                CancellationToken token = default
            )
        {
            await _db.PlanejamentosRecebimento.AddAsync(recebimento, token);
            await SaveChangesAsync(token);
            return recebimento;
        }

        public async Task<PlanejamentoRecebimento> GetById(Guid id, CancellationToken token = default)
        {
            var recebimento = await _db.PlanejamentosRecebimento
                .Include(x => x.Fornecedor)
                .Include(x => x.ItemPlanejamentos)
                .FirstOrDefaultAsync(x=> x.Id == id, token);
            
            return recebimento!;
        }

        public async Task<PlanejamentoRecebimento> UpdateRecebimento
            (   
                Guid id,
                PlanejamentoRecebimento recebimento,
                CancellationToken token = default
            )
        {
            var recebimentoBuscado = await GetById(id, token);

            recebimentoBuscado.Fornecedor = recebimento.Fornecedor;
            recebimentoBuscado.FornecedorId = recebimento.FornecedorId;
            recebimentoBuscado.StatusRecebimento = recebimento.StatusRecebimento;
            recebimentoBuscado.ItemPlanejamentos = recebimento.ItemPlanejamentos;
            recebimentoBuscado.DataInicio = recebimento.DataInicio;
            recebimentoBuscado.CreatedAt = recebimento.CreatedAt;
            recebimentoBuscado.UpdatedAt = DateTime.UtcNow;

            await SaveChangesAsync(token);
            return recebimentoBuscado;
        }

        public async Task DeleteRecebimento(Guid id, CancellationToken token = default)
        {
            var recebimento = await GetById(id, token);
            _db.Remove(recebimento);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
