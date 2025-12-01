using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
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
            return grade;
        }

        public async Task<List<Grade>> GetAll(CancellationToken token = default)
        {
            return await _db.Grade
                .Include(x => x.Produto)
                .Include(x => x.Fornecedor)
                .Include(x => x.DataInicio)
                .Include(x => x.DataFim)
                .Include(x => x.HoraInicial)
                .Include(x => x.HoraFinal)
                .Include(x => x.IntervaloMinutos)
                .ToListAsync(token);
        }

        public async Task<Grade> GetById(Guid id, CancellationToken token = default)
        {
            var grade = await _db.Grade
                .Include(x => x.Produto)
                .Include(x => x.Fornecedor)
                .Include(x => x.DataInicio)
                .Include(x => x.DataFim)
                .Include(x => x.HoraInicial)
                .Include(x => x.HoraFinal)
                .Include(x => x.IntervaloMinutos)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return grade!;
        }

        public async Task<Grade> Update(Guid id, Grade grade, CancellationToken token = default)
        {
            var gradeEncontrada = await GetById(id, token);

            gradeEncontrada.Id = grade.Id;
            gradeEncontrada.Produto = grade.Produto;
            gradeEncontrada.Fornecedor = grade.Fornecedor;
            gradeEncontrada.DataInicio = grade.DataInicio;
            gradeEncontrada.DataFim = grade.DataFim;
            gradeEncontrada.HoraInicial = grade.HoraInicial;
            gradeEncontrada.HoraFinal = grade.HoraFinal;
            gradeEncontrada.IntervaloMinutos = grade.IntervaloMinutos;

            await SaveChangesAsync(token);
            return gradeEncontrada;
        }

        public async Task Delete(Guid id, CancellationToken token)
        {
            var gradeEncontrada = await GetById(id, token);

            _db.Remove(gradeEncontrada);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
