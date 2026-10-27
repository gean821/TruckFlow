using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public sealed class RelatorioBiRepositorio : IRelatorioBiRepositorio
    {
        private readonly AppDbContext _db;

        public RelatorioBiRepositorio(AppDbContext db) => _db = db;

        public async Task<List<RelatorioBiLinha>> ListarLinhasAsync(
            RelatorioBiCriteria criteria,
            CancellationToken token = default)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Where(Agendamento.TemReserva)
                .Where(a => a.DataInicio >= criteria.DesdeUtc && a.DataInicio < criteria.AteUtc);

            if (criteria.UnidadeEntregaId.HasValue)
                query = query.Where(a => a.UnidadeEntregaId == criteria.UnidadeEntregaId);

            if (criteria.LocalDescargaId.HasValue)
                query = query.Where(a => a.LocalDescargaId == criteria.LocalDescargaId
                    || (a.LocalDescargaId == null && a.Grade != null && a.Grade.LocalDescargaId == criteria.LocalDescargaId));

            if (criteria.FornecedorId.HasValue)
                query = query.Where(a => a.FornecedorId == criteria.FornecedorId
                    || (a.NotaFiscal != null && a.NotaFiscal.FornecedorId == criteria.FornecedorId));

            if (criteria.ProdutoId.HasValue)
                query = query.Where(a => a.ProdutoId == criteria.ProdutoId
                    || (a.Grade != null && a.Grade.ProdutoId == criteria.ProdutoId));

            return await query
                .Select(a => new RelatorioBiLinha(
                    a.Id,
                    a.StatusAgendamento,
                    a.DataInicio,
                    a.DataFim,
                    a.NotaFiscal != null ? (a.NotaFiscal.PesoBruto ?? a.VolumeCarga ?? 0m) : (a.VolumeCarga ?? 0m),
                    a.FornecedorId ?? (a.NotaFiscal != null ? a.NotaFiscal.FornecedorId : null),
                    a.Fornecedor != null
                        ? a.Fornecedor.Nome
                        : (a.NotaFiscal != null && a.NotaFiscal.Fornecedor != null ? a.NotaFiscal.Fornecedor.Nome : null),
                    a.ProdutoId ?? (a.Grade != null ? a.Grade.ProdutoId : null),
                    a.Produto != null ? a.Produto.Nome : (a.Grade != null ? a.Grade.Produto.Nome : null),
                    a.UnidadeEntregaId,
                    a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    a.LocalDescargaId ?? (a.Grade != null ? a.Grade.LocalDescargaId : null),
                    a.LocalDescarga != null
                        ? a.LocalDescarga.Nome
                        : (a.Grade != null && a.Grade.LocalDescarga != null ? a.Grade.LocalDescarga.Nome : null),
                    a.ChegouEm,
                    a.InicioDescargaEm,
                    a.FimDescargaEm,
                    a.SaiuEm,
                    a.PlacaVeiculo ?? (a.NotaFiscal != null ? a.NotaFiscal.PlacaVeiculo : null),
                    a.Usuario != null && a.Usuario.Motorista != null ? a.Usuario.Motorista.NomeReal : null))
                .ToListAsync(token);
        }

        public async Task<List<RelatorioBiDoca>> ListarDocasAtivasAsync(
            Guid? unidadeEntregaId,
            CancellationToken token = default)
        {
            var query = _db.LocalDescarga.AsNoTracking().Where(l => l.Ativa != false);

            if (unidadeEntregaId.HasValue)
                query = query.Where(l => l.UnidadeEntregaId == unidadeEntregaId);

            return await query
                .OrderBy(l => l.UnidadeEntrega.Nome)
                .ThenBy(l => l.Nome)
                .Select(l => new RelatorioBiDoca(l.Id, l.Nome, l.UnidadeEntrega.Nome))
                .ToListAsync(token);
        }

        public async Task<int> ContarDocasAtivasAsync(
            Guid? unidadeEntregaId,
            Guid? localDescargaId,
            CancellationToken token = default)
        {
            var query = _db.LocalDescarga.AsNoTracking().Where(l => l.Ativa != false);

            if (unidadeEntregaId.HasValue)
                query = query.Where(l => l.UnidadeEntregaId == unidadeEntregaId);

            if (localDescargaId.HasValue)
                query = query.Where(l => l.Id == localDescargaId);

            return await query.CountAsync(token);
        }
    }
}
