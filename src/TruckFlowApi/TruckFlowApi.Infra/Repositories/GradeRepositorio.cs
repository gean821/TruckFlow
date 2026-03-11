using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Grade;

namespace TruckFlowApi.Infra.Repositories
{
    public class GradeRepositorio : IGradeRepositorio
    {
        private readonly AppDbContext _db;

        public GradeRepositorio(AppDbContext db) => _db = db;

        public async Task<Grade> CreateGrade(Grade grade, CancellationToken token = default)
        {
            await _db.Grade.AddAsync(grade, token);
            await SaveChangesAsync(token);
            return grade;
        }

        public async Task<List<Grade>> GetAll(CancellationToken token = default)
        {
            return await _db.Grade
                .Include(x => x.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Fornecedor)
                .Include(x=> x.LocalDescarga)
                .Include(x=> x.UnidadeEntrega)
                .ToListAsync(token);
        }

        public async Task<PagedResponse<Grade>> GetPagedAsync(
            GradeListQueryDto query,
            CancellationToken token = default)
        {
            var dbQuery = _db.Grade
                .AsNoTracking()
                .Include(x => x.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Fornecedor)
                .Include(x => x.LocalDescarga)
                .AsQueryable();

            if (query.ProdutoId.HasValue)
                dbQuery = dbQuery.Where(x => x.ProdutoId == query.ProdutoId.Value);

            if (query.FornecedorId.HasValue)
                dbQuery = dbQuery.Where(x => x.FornecedorId == query.FornecedorId.Value);

            if (query.LocalDescargaId.HasValue)
                dbQuery = dbQuery.Where(x => x.LocalDescargaId == query.LocalDescargaId.Value);

            if (query.DataInicio.HasValue)
                dbQuery = dbQuery.Where(x => x.DataInicio >= query.DataInicio.Value);

            if (query.DataFim.HasValue)
                dbQuery = dbQuery.Where(x => x.DataFim <= query.DataFim.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                dbQuery = dbQuery.Where(x =>
                    x.Produto.Nome.Contains(search) ||
                    x.Fornecedor.Nome.Contains(search) ||
                    (x.LocalDescarga != null && x.LocalDescarga.Nome.Contains(search)) ||
                    (x.UnidadeEntrega != null && x.UnidadeEntrega.Nome.Contains(search)));
            }

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var totalCount = await dbQuery.CountAsync(token);

            var items = await dbQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return new PagedResponse<Grade>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<Grade?> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
             return await _db.Grade
               .Include(x => x.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Fornecedor)
                .Include(x => x.LocalDescarga)
                .Include(x => x.UnidadeEntrega)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }

        public async Task<Grade> Update(
            Grade grade,
            CancellationToken token = default
            )
        {
            _db.Grade.Update(grade);
            await SaveChangesAsync(token);

            return grade;
        }

        public async Task<bool> CheckAppointmentExistence(
            Guid gradeId,
            CancellationToken token = default
            )
        {
            return await _db.Agendamento
                .AsNoTracking()
                .AnyAsync(
                    x => x.GradeId == gradeId
                    && x.StatusAgendamento != StatusAgendamento.Disponivel,
                    token
                );
        }

        public async Task Delete(
            Grade grade,
            CancellationToken token = default
            )
        {
            _db.Remove(grade);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
