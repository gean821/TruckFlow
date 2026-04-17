using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class PlanejamentoRecebimentoRepositorio : IPlanejamentoRecebimentoRepositorio
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
                    .ThenInclude(x=> x.Produto)
                .FirstOrDefaultAsync(x=> x.Id == id, token);

            return recebimento!;
        }

        public async Task<PlanejamentoRecebimento?> GetByIdWithEventos(Guid id, CancellationToken token = default)
        {
            return await _db.PlanejamentosRecebimento
                .Include(x => x.Fornecedor)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(i => i.Produto)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(i => i.RecebimentoEventos)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }

        public async Task<PagedResponse<PlanejamentoRecebimento>> GetPagedAsync(
            PlanejamentoListQueryDto query,
            CancellationToken token = default)
        {
            var dbQuery = _db.PlanejamentosRecebimento
                .AsNoTracking()
                .Include(x => x.Fornecedor)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(i => i.Produto)
                .AsQueryable();

            if (query.FornecedorId.HasValue)
                dbQuery = dbQuery.Where(x => x.FornecedorId == query.FornecedorId.Value);

            if (query.Status.HasValue)
                dbQuery = dbQuery.Where(x => x.StatusRecebimento == query.Status.Value);

            if (query.ProdutoId.HasValue)
                dbQuery = dbQuery.Where(x =>
                    x.ItemPlanejamentos.Any(i => i.ProdutoId == query.ProdutoId.Value));

            if (query.DataInicio.HasValue)
                dbQuery = dbQuery.Where(x => x.DataFim >= query.DataInicio.Value);

            if (query.DataFim.HasValue)
                dbQuery = dbQuery.Where(x => x.DataInicio <= query.DataFim.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                dbQuery = dbQuery.Where(x =>
                    x.Fornecedor.Nome.Contains(search) ||
                    x.ItemPlanejamentos.Any(i => i.Produto.Nome.Contains(search)));
            }

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var totalCount = await dbQuery.CountAsync(token);

            var items = await dbQuery
                .OrderByDescending(x => x.DataInicio)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return new PagedResponse<PlanejamentoRecebimento>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
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
        public async Task<PlanejamentoRecebimento?> GetPlanejamentoAtivoPorFornecedor(
             Guid fornecedorId,
             CancellationToken token = default)
        {
            var hoje = DateTime.UtcNow.Date;

            return await _db.PlanejamentosRecebimento
                .Include(x => x.Fornecedor)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(i => i.Produto)
                .Include(x => x.ItemPlanejamentos)
                    .ThenInclude(i => i.RecebimentoEventos)
                .Where(x =>
                    x.FornecedorId == fornecedorId &&
                    (x.StatusRecebimento == StatusRecebimento.Planejado ||
                     x.StatusRecebimento == StatusRecebimento.EmAndamento) &&
                    x.DataInicio.Date <= hoje &&
                    x.DataFim.Date >= hoje
                )
                .FirstOrDefaultAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }

    }
}
