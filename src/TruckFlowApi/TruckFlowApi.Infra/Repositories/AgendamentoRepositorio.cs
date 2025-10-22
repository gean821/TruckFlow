using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public sealed class AgendamentoRepositorio : IAgendamentoRepositorio
    {
        private readonly AppDbContext _db;

        public AgendamentoRepositorio(AppDbContext db) => _db = db;
      
        public async Task<Agendamento> AddAgendamento(
            Agendamento agendamento,
            CancellationToken token = default)
        {
            await _db.Agendamento.AddAsync(agendamento, token);
            await SaveChangesAsync(token);
            return agendamento;
        }
        public async Task<Agendamento> GetById(Guid id, CancellationToken token = default)
        {
            var agendamento = await _db.Agendamento.Include(x => x.FornecedorId)
                .Include(x => x.TipoCarga)
                .Include(x => x.VolumeCarga)
                .Include(x => x.UnidadeEntregaId)
                .Include(x => x.NotaFiscalId)
                .Include(x => x.StatusAgendamento)
                .Include(x => x.UsuarioId)
                .Include(x => x.Notificacoes)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return agendamento!;
        }

        public async Task Delete(Guid id, CancellationToken token = default)
        {
            var agendamento = await GetById(id, token);
            _db.Agendamento.Remove(agendamento);
            await SaveChangesAsync(token);
        }

        public async Task<List<Agendamento>> GetAll(CancellationToken token = default)
        {
            return await _db.Agendamento.Include(x => x.FornecedorId)
                .Include(x => x.TipoCarga)
                .Include(x => x.VolumeCarga)
                .Include(x => x.UnidadeEntregaId)
                .Include(x => x.NotaFiscalId)
                .Include(x => x.StatusAgendamento)
                .Include(x => x.UsuarioId)
                .Include(x => x.Notificacoes)
                .ToListAsync(token);
        }

        public async Task<Agendamento> Update(
            Guid id,
            Agendamento agendamento,
            CancellationToken token = default)
        {
            var agendamentoEncontrado = await GetById(id, token);

            agendamentoEncontrado.UsuarioId = agendamento.UsuarioId;
            agendamentoEncontrado.FornecedorId = agendamento.FornecedorId;
            agendamentoEncontrado.TipoCarga = agendamento.TipoCarga;
            agendamentoEncontrado.VolumeCarga = agendamento.VolumeCarga;
            agendamentoEncontrado.StatusAgendamento = agendamento.StatusAgendamento;
            agendamentoEncontrado.UnidadeEntregaId = agendamento.UnidadeEntregaId;
            agendamentoEncontrado.NotaFiscalId = agendamento.NotaFiscalId;

            await SaveChangesAsync(token);

            return agendamentoEncontrado;
        }
        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
