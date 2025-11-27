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
    public class NotaFiscalRepositorio : INotaFiscalRepositorio
    {
        private readonly AppDbContext _db;
        public NotaFiscalRepositorio(AppDbContext db) => _db = db;


        public async Task<NotaFiscal> SaveParsedNotaAsync(
            NotaFiscal nota,
            CancellationToken token
            )
        {
            await _db.NotaFiscal.AddAsync(nota, token);
            await SaveChangesAsync(token);
            return nota;
        }
        public async Task<NotaFiscal?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token)
        {
            return await _db.NotaFiscal
                .Include(x=> x.FornecedorId)
                .Include(x=> x.Itens)
                .Include(x=> x.AgendamentoId)
                .FirstOrDefaultAsync(x => x.ChaveAcesso == chaveAcesso, token);
        }
        
        public async Task SaveChangesAsync(CancellationToken token)
        {
            await _db.SaveChangesAsync(token);
        }

    }
}
