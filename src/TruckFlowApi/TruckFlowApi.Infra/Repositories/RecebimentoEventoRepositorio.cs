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

        public async Task SaveChangeAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
