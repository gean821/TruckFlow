using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public sealed class AgendamentoRepositorio(AppDbContext db) : IAgendamentoRepositorio
    {
        private readonly AppDbContext _db = db;

        public async Task<Agendamento> AddAgendamento(
            Agendamento agendamento,
            CancellationToken token = default)
        {
            await _db.Agendamento.AddAsync(agendamento, token);
            return agendamento;
        }
        public async Task AddRangeAsync(
            List<Agendamento> agendamentos,
            CancellationToken token = default
            )
        {
            await _db.Agendamento.AddRangeAsync(agendamentos, token);
        }
        public async Task<List<Agendamento>> GetAvailable(
            Guid fornecedorId,
            DateTime dataInicio,
            DateTime dataFim,
            CancellationToken token = default
            )
        {
            var agora = DateTime.UtcNow;
            Console.WriteLine($"[DEBUG] Buscando vagas para FornecedorId: {fornecedorId} entre {dataInicio} e {dataFim}");

            IQueryable<Agendamento> query = _db.Agendamento
                .Where(x =>
                    x.FornecedorId == fornecedorId &&
                    x.StatusAgendamento == StatusAgendamento.Disponivel &&
                    x.DataInicio >= dataInicio &&
                    x.DataInicio <= dataFim
                );

            return await query
                .OrderBy(x => x.DataInicio)
                .Include(x => x.Grade)
                    .ThenInclude(x => x.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Fornecedor)
                .ToListAsync(token);
        }

        public async Task<List<AgendamentoAdminResponse>> GetAdminViewAsync(
       DateTime dataInicio,
       DateTime dataFim,
       Guid? fornecedorId,
       Guid? unidadeEntregaId,
       CancellationToken cancellationToken = default)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Include(x => x.Fornecedor)
                .Include(x => x.Grade)
                    .ThenInclude(g => g.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Usuario)
                    .ThenInclude(u => u.Motorista)
                .Include(x => x.NotaFiscal)
                .Where(x => x.DataInicio >= dataInicio && x.DataInicio <= dataFim);

            if (fornecedorId.HasValue)
            {
                query = query.Where(x => x.FornecedorId == fornecedorId);
            }

            if (unidadeEntregaId.HasValue)
            {
                query = query.Where(x => x.UnidadeEntregaId == unidadeEntregaId);
            }

            return await query
                .Select(x => new AgendamentoAdminResponse
                {
                    Id = x.Id,
                    FornecedorNome = x.Fornecedor.Nome,
                    MotoristaNome = x.Usuario != null
                                     ? x.Usuario.Motorista != null
                                     ? x.Usuario.Motorista.NomeReal
                                    : null
                                    : null,

                    DataInicio = x.DataInicio,
                    DataFim = x.DataFim,
                    Produto = x.Grade != null
                                ? x.Grade.Produto.Nome : x.TipoCarga.ToString(),

                    PesoCarga = x.NotaFiscal!.PesoBruto ?? x.VolumeCarga ?? 0m,
                    PlacaVeiculo = x.PlacaVeiculo ?? x.NotaFiscal.PlacaVeiculo,
                    UnidadeEntrega = x.UnidadeEntrega.Localizacao,
                    Status = x.StatusAgendamento.ToString(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    DeletedAt = x.DeletedAt
                })
                .OrderBy(x => x.DataInicio)
                .ToListAsync(cancellationToken);
        }

        public async Task<Agendamento> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            var agendamento = await _db.Agendamento
                .Include(x => x.Fornecedor)
                .Include(x => x.Usuario)
                .Include(x => x.NotaFiscal)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Grade)
                    .ThenInclude(x => x!.Produto)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return agendamento!;
        }

        public async Task<List<Agendamento>> GetByMotoristaId(
            Guid motoristaId,
            CancellationToken token = default
            )
        {
            return await _db.Agendamento
                .Where(x => x.Usuario.Motorista.Id == motoristaId)
                .OrderByDescending(x => x.DataInicio)
                .Include(x => x.Fornecedor)
                .Include(x => x.NotaFiscal)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Grade).ThenInclude(g => g.Produto)
                .ToListAsync(token);
        }

        public async Task<bool> ExisteAgendamentoBloqueantePorGrade(
            Guid gradeId,
            CancellationToken cancellationToken = default
            )
        {
            return await _db.Agendamento
                .AnyAsync(a =>
                    a.GradeId == gradeId &&
                    a.StatusAgendamento != StatusAgendamento.Disponivel &&
                    a.StatusAgendamento != StatusAgendamento.Cancelado,
                    cancellationToken);
        }
        public async Task Delete(
            Agendamento agendamento,
            CancellationToken token = default
            )
        {
            _db.Agendamento.Remove(agendamento);
            await SaveChangesAsync(token);
        }

        public async Task<List<Agendamento>> GetAll(CancellationToken token = default)
        {
            return await _db.Agendamento
                .Include(x => x.Fornecedor)
                .Include(x => x.Usuario)
                .Include(x => x.NotaFiscal)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Grade)
                        .ThenInclude(g => g.Produto)
                .ToListAsync(token);
        }

        public async Task<Agendamento> Update(
            Agendamento agendamento,
            CancellationToken token = default
            )
        {
            _db.Agendamento.Update(agendamento);
            await SaveChangesAsync(token);
            return agendamento;
        }
        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }

        public async Task<Agendamento?> GetByIdWithFornecedor(
            Guid id,
            CancellationToken token = default
            )
        {
            return await _db.Agendamento
                .Include(x => x.Fornecedor)
                .ThenInclude(x => x.Recebimentos)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }
    }
}
