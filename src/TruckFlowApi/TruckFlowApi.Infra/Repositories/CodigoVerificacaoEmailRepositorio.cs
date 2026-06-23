using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;

namespace TruckFlowApi.Infra.Repositories
{
    public class CodigoVerificacaoEmailRepositorio : ICodigoVerificacaoEmailRepositorio
    {
        private readonly AppDbContext _db;

        public CodigoVerificacaoEmailRepositorio(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CodigoVerificacaoEmail?> ObterUltimoAtivoAsync(
            Guid usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            CancellationToken token = default)
        {
            var agora = DateTime.UtcNow;
            return await _db.CodigoVerificacaoEmail
                .Where(x => x.UsuarioId == usuarioId
                    && x.Finalidade == finalidade
                    && x.UsadoEm == null
                    && x.ExpiraEm > agora
                    && x.Tentativas < 3)
                .OrderByDescending(x => x.CriadoEm)
                .FirstOrDefaultAsync(token);
        }

        public async Task<int> ContarEnviosRecentesAsync(
            Guid usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            TimeSpan janela,
            CancellationToken token = default)
        {
            var limite = DateTime.UtcNow - janela;
            return await _db.CodigoVerificacaoEmail
                .CountAsync(x => x.UsuarioId == usuarioId
                    && x.Finalidade == finalidade
                    && x.CriadoEm >= limite, token);
        }

        public async Task InvalidarAnterioresAsync(
            Guid usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            CancellationToken token = default)
        {
            var agora = DateTime.UtcNow;
            await _db.CodigoVerificacaoEmail
                .Where(x => x.UsuarioId == usuarioId
                    && x.Finalidade == finalidade
                    && x.UsadoEm == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsadoEm, agora), token);
        }

        public async Task AdicionarAsync(
            CodigoVerificacaoEmail codigo,
            CancellationToken token = default)
        {
            await _db.CodigoVerificacaoEmail.AddAsync(codigo, token);
        }

        public async Task SalvarAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
