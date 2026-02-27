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
                .ToListAsync(token);
        }

        public async Task<Grade?> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
             return await _db.Grade
                .Include(x => x.Produto)
                .Include(x => x.Fornecedor)
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
