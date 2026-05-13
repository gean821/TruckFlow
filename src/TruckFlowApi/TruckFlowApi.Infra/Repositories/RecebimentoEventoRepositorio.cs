using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
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
                    .ThenInclude(i => i!.PlanejamentoRecebimento)
                .FirstOrDefaultAsync(x => x.AgendamentoId == agendamentoId, token);
        }

        public async Task<RecebimentoEvento?> GetByAgendamentoIdETipo(
            Guid agendamentoId,
            TipoMovimentoRecebimento tipo,
            CancellationToken token = default)
        {
            return await _db.RecebimentoEvento
                .Include(x => x.ItemPlanejamento)
                    .ThenInclude(i => i!.PlanejamentoRecebimento)
                .FirstOrDefaultAsync(
                    x => x.AgendamentoId == agendamentoId && x.Tipo == tipo,
                    token);
        }

        public async Task<List<RecebimentoEvento>> GetByAgendamentoIdAll(
            Guid agendamentoId,
            CancellationToken token = default)
        {
            return await _db.RecebimentoEvento
                .Include(x => x.ItemPlanejamento)
                    .ThenInclude(i => i!.PlanejamentoRecebimento)
                .Where(x => x.AgendamentoId == agendamentoId)
                .ToListAsync(token);
        }

        public async Task<PagedResponse<RecebimentoEvento>> GetOrfaosPagedAsync(
            RecebimentoOrfaoQueryDto query,
            CancellationToken token = default)
        {
            var dbQuery = _db.RecebimentoEvento
                .AsNoTracking()
                .Include(x => x.Produto)
                .Include(x => x.Fornecedor)
                .Include(x => x.Agendamento)
                .Where(x => x.ItemPlanejamentoId == null);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                dbQuery = dbQuery.Where(x =>
                    (x.Produto != null && x.Produto.Nome.Contains(search)) ||
                    (x.Fornecedor != null && x.Fornecedor.Nome.Contains(search)) ||
                    (x.Observacao != null && x.Observacao.Contains(search)));
            }

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var totalCount = await dbQuery.CountAsync(token);

            var items = await dbQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return new PagedResponse<RecebimentoEvento>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<RecebimentoEvento?> GetById(Guid id, CancellationToken token = default)
        {
            return await _db.RecebimentoEvento
                .Include(x => x.ItemPlanejamento)
                    .ThenInclude(i => i!.PlanejamentoRecebimento)
                .FirstOrDefaultAsync(x => x.Id == id, token);
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
