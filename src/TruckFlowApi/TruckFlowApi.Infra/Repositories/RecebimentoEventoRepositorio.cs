using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class RecebimentoEventoRepositorio : IRecebimentoEventoRepositorio
    {
        private readonly AppDbContext _db;

        public RecebimentoEventoRepositorio(AppDbContext db) => _db = db;

        public async Task<RecebimentoEvento> AddAsync(RecebimentoEvento evento, CancellationToken token = default)
        {
            await _db.RecebimentoEvento.AddAsync(evento, token);
            await SaveChangeAsync(token);
            return evento;
        }

        public async Task<RecebimentoEvento?> GetByAgendamentoId(Guid agendamentoId, CancellationToken token = default)
        {
            return await _db.RecebimentoEvento
                .Include(x => x.ItemPlanejamento)
                    .ThenInclude(i => i.PlanejamentoRecebimento)
                .FirstOrDefaultAsync(x => x.AgendamentoId == agendamentoId, token);
        }

        public async Task Remove(RecebimentoEvento evento, CancellationToken token = default)
        {
            _db.RecebimentoEvento.Remove(evento);
            await SaveChangeAsync(token);
        }

        public async Task SaveChangeAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
