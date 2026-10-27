using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
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
            return await Filtrar(_db.Agendamento.AsNoTracking(), criteria)
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

        public async Task<List<RecebimentoLinha>> ListarRecebimentosAsync(
            RelatorioBiCriteria criteria,
            CancellationToken token = default)
        {
            var query = Filtrar(_db.Agendamento.AsNoTracking(), criteria)
                .Where(a => a.StatusAgendamento == StatusAgendamento.Finalizado);

            return await query
                .Select(a => new RecebimentoLinha(
                    a.Id,
                    a.DataInicio,
                    a.NotaFiscal != null ? (a.NotaFiscal.PesoBruto ?? a.VolumeCarga ?? 0m) : (a.VolumeCarga ?? 0m),
                    _db.RecebimentoEvento.Any(e => e.AgendamentoId == a.Id && e.Tipo == TipoMovimentoRecebimento.Confirmacao)
                        ? _db.RecebimentoEvento
                            .Where(e => e.AgendamentoId == a.Id && e.Tipo == TipoMovimentoRecebimento.Confirmacao)
                            .Sum(e => e.Quantidade)
                        : (decimal?)null,
                    a.ProdutoId ?? (a.Grade != null ? a.Grade.ProdutoId : null),
                    a.Produto != null ? a.Produto.Nome : (a.Grade != null ? a.Grade.Produto.Nome : null),
                    a.FornecedorId ?? (a.NotaFiscal != null ? a.NotaFiscal.FornecedorId : null),
                    a.Fornecedor != null
                        ? a.Fornecedor.Nome
                        : (a.NotaFiscal != null && a.NotaFiscal.Fornecedor != null ? a.NotaFiscal.Fornecedor.Nome : null),
                    a.UnidadeEntregaId,
                    a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    a.PlacaVeiculo ?? (a.NotaFiscal != null ? a.NotaFiscal.PlacaVeiculo : null),
                    a.NotaFiscal != null ? a.NotaFiscal.Numero : null))
                .ToListAsync(token);
        }

        public async Task<List<PlanejamentoItemLinha>> ListarPlanejamentoAsync(
            RelatorioBiCriteria criteria,
            DateTime periodoInicioUtc,
            CancellationToken token = default)
        {
            var query = _db.ItensPlanejamento
                .AsNoTracking()
                .Where(i => i.PlanejamentoRecebimento.DataInicio < criteria.AteUtc
                    && i.PlanejamentoRecebimento.DataFim >= periodoInicioUtc);

            if (criteria.FornecedorId.HasValue)
                query = query.Where(i => i.PlanejamentoRecebimento.FornecedorId == criteria.FornecedorId);

            if (criteria.ProdutoId.HasValue)
                query = query.Where(i => i.ProdutoId == criteria.ProdutoId);

            return await query
                .Select(i => new PlanejamentoItemLinha(
                    i.Id,
                    i.ProdutoId,
                    i.Produto.Nome,
                    i.PlanejamentoRecebimento.FornecedorId,
                    i.PlanejamentoRecebimento.Fornecedor.Nome,
                    i.PlanejamentoRecebimento.DataInicio,
                    i.PlanejamentoRecebimento.DataFim,
                    i.CadenciaDiariaPlanejada,
                    i.DiasSemana,
                    i.QuantidadeTotalPlanejada,
                    _db.RecebimentoEvento
                        .Where(e => e.ItemPlanejamentoId == i.Id
                            && e.Tipo == TipoMovimentoRecebimento.Confirmacao
                            && e.DataRecebimento >= periodoInicioUtc
                            && e.DataRecebimento < criteria.AteUtc)
                        .Sum(e => e.Quantidade)))
                .ToListAsync(token);
        }

        public async Task<(int Quantidade, decimal Kg)> ResumirOrfaosAsync(
            RelatorioBiCriteria criteria,
            DateTime periodoInicioUtc,
            CancellationToken token = default)
        {
            var query = _db.RecebimentoEvento
                .AsNoTracking()
                .Where(e => e.ItemPlanejamentoId == null
                    && e.Tipo == TipoMovimentoRecebimento.Confirmacao
                    && e.DataRecebimento >= periodoInicioUtc
                    && e.DataRecebimento < criteria.AteUtc);

            if (criteria.FornecedorId.HasValue)
                query = query.Where(e => e.FornecedorId == criteria.FornecedorId);

            if (criteria.ProdutoId.HasValue)
                query = query.Where(e => e.ProdutoId == criteria.ProdutoId);

            var resumo = await query
                .GroupBy(_ => 1)
                .Select(g => new { Quantidade = g.Count(), Kg = g.Sum(e => e.Quantidade) })
                .FirstOrDefaultAsync(token);

            return resumo is null ? (0, 0m) : (resumo.Quantidade, resumo.Kg);
        }

        private IQueryable<Agendamento> Filtrar(IQueryable<Agendamento> query, RelatorioBiCriteria criteria)
        {
            query = query
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

            return query;
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
