using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Dto.RelatorioMotorista;
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
            await SaveChangesAsync(token);
            return agendamento;
        }
        public async Task AddRangeAsync(
            List<Agendamento> agendamentos,
            CancellationToken token = default
            )
        {
            await _db.Agendamento.AddRangeAsync(agendamentos, token);
        }
        public async Task<List<Agendamento>> GetAvailableByProdutos(
            IReadOnlyCollection<Guid> produtoIds,
            Guid? fornecedorIdNota,
            DateTime dataInicio,
            DateTime dataFim,
            Guid empresaId,
            CancellationToken token = default
            )
        {
            var agora = DateTime.UtcNow;

            return await _db.Agendamento
                .AsNoTracking()
                .Include(x => x.Grade)
                    .ThenInclude(g => g!.Produto)
                .Include(x => x.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Fornecedor)
                .Where(x => x.EmpresaId == empresaId)
                .Where(x => x.StatusAgendamento == StatusAgendamento.Disponivel)
                .Where(x => x.DataInicio >= dataInicio && x.DataInicio <= dataFim)
                .Where(x => x.DataFim > agora)
                .Where(x =>
                    (x.ProdutoId.HasValue && produtoIds.Contains(x.ProdutoId.Value)) ||
                    (x.Grade != null && produtoIds.Contains(x.Grade.ProdutoId))
                )
                .Where(x => x.FornecedorId == null || x.FornecedorId == fornecedorIdNota)
                .OrderBy(x => x.DataInicio)
                .ToListAsync(token);
        }

        public async Task<PagedResponse<AgendamentoAdminResponse>> GetAdminViewAsync(
            AgendamentoFilterDto filtros,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Include(x => x.Fornecedor)
                .Include(x => x.Produto)
                .Include(x => x.Grade)
                    .ThenInclude(g => g.Produto)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.Usuario)
                    .ThenInclude(u => u.Motorista)
                .Include(x => x.NotaFiscal)
                    .ThenInclude(n => n!.Fornecedor)
                .AsQueryable();

            if (filtros.DataInicio.HasValue)
            {
                query = query.Where(x => x.DataInicio >= filtros.DataInicio.Value);
            }

            if (filtros.DataFim.HasValue)
            {
                query = query.Where(x => x.DataInicio <= filtros.DataFim.Value);
            }

            if (filtros.FornecedorId.HasValue)
            {
                query = query.Where(x => x.FornecedorId == filtros.FornecedorId.Value);
            }

            if (filtros.UnidadeEntregaId.HasValue)
            {
                query = query.Where(x => x.UnidadeEntregaId == filtros.UnidadeEntregaId.Value);
            }

            if (filtros.LocalDescargaId.HasValue)
            {
                query = query.Where(x => x.LocalDescargaId == filtros.LocalDescargaId.Value
                    || (x.LocalDescargaId == null && x.Grade != null && x.Grade.LocalDescargaId == filtros.LocalDescargaId.Value));
            }

            if (filtros.ProdutoId.HasValue)
            {
                var produtoId = filtros.ProdutoId.Value;
                query = query.Where(x =>
                    x.ProdutoId == produtoId ||
                    (x.Grade != null && x.Grade.ProdutoId == produtoId));
            }

            if (filtros.Status.HasValue)
            {
                query = query.Where(x => x.StatusAgendamento == filtros.Status.Value);
            }

            if (filtros.TipoVeiculo.HasValue)
            {
                query = query.Where(x => x.TipoVeiculo == filtros.TipoVeiculo.Value);
            }

            if (filtros.OcultarVagasLivres)
            {
                query = query.Where(Agendamento.TemReserva);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Search))
            {
                var search = filtros.Search.Trim();

                query = query.Where(x =>
                    (x.PlacaVeiculo != null && x.PlacaVeiculo.Contains(search)) ||
                    (x.NotaFiscal != null && x.NotaFiscal.PlacaVeiculo != null && x.NotaFiscal.PlacaVeiculo.Contains(search)) ||
                    x.Fornecedor.Nome.Contains(search) ||
                    (x.UnidadeEntrega != null && x.UnidadeEntrega.Nome.Contains(search)) ||
                    (x.Produto != null && x.Produto.Nome.Contains(search)) ||
                    (x.Grade != null && x.Grade.Produto.Nome.Contains(search)) ||
                    (x.Usuario != null && x.Usuario.Motorista != null && x.Usuario.Motorista.NomeReal.Contains(search)));
            }

            var pageNumber = filtros.PageNumber <= 0 ? 1 : filtros.PageNumber;
            var pageSize = filtros.PageSize <= 0 ? 20 : filtros.PageSize;

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.DataInicio)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AgendamentoAdminResponse
                {
                    Id = x.Id,
                    FornecedorNome = x.Fornecedor != null
                        ? x.Fornecedor.Nome
                        : (x.NotaFiscal != null && x.NotaFiscal.Fornecedor != null
                            ? x.NotaFiscal.Fornecedor.Nome
                            : null),
                    MotoristaNome = x.Usuario != null
                                     ? x.Usuario.Motorista != null
                                        ? x.Usuario.Motorista.NomeReal
                                        : null
                                     : null,
                    MotoristaTelefone = x.Usuario != null
                                        ? x.Usuario.Motorista != null
                                           ? x.Usuario.Motorista.Telefone
                                           : null
                                        : null,
                    DataInicio = x.DataInicio,
                    DataFim = x.DataFim,
                    Produto = x.Grade != null
                                ? x.Grade.Produto.Nome
                                : (x.Produto != null ? x.Produto.Nome : x.TipoCarga.ToString()),
                    PesoCarga = x.NotaFiscal != null
                                ? (x.NotaFiscal.PesoBruto ?? x.VolumeCarga ?? 0m)
                                : (x.VolumeCarga ?? 0m),
                    PlacaVeiculo = x.PlacaVeiculo ?? (x.NotaFiscal != null ? x.NotaFiscal.PlacaVeiculo : null),
                    TipoVeiculo = x.TipoVeiculo.HasValue ? x.TipoVeiculo.Value.ToString() : null,
                    UnidadeEntrega = x.UnidadeEntrega != null ? x.UnidadeEntrega.Nome : null,
                    Status = x.StatusAgendamento.ToString(),
                    CreatedAt = x.CreatedAt,
                    ChegouEm = x.ChegouEm,
                    FimDescargaEm = x.FimDescargaEm,
                    SaiuEm = x.SaiuEm,
                    UpdatedAt = x.UpdatedAt,
                    DeletedAt = x.DeletedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<AgendamentoAdminResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
         
        private const string FusoOperacionalIana = "America/Sao_Paulo";

        public Task<int> CountLinhasRelatorioAsync(
            RelatorioAgendamentoCriteria criteria,
            CancellationToken cancellationToken = default) => 
            FiltrarRelatorio(criteria).CountAsync(cancellationToken);

        public Task<List<AgendamentoRelatorioLinha>> GetLinhasRelatorioAsync(
           RelatorioAgendamentoCriteria criteria,
           int limite,
           CancellationToken cancellationToken = default) =>
           ProjetarRelatorio(FiltrarRelatorio(criteria), criteria, limite)
               .ToListAsync(cancellationToken);

        public IAsyncEnumerable<AgendamentoRelatorioLinha> StreamLinhasRelatorioAsync(
            RelatorioAgendamentoCriteria criteria,
            int limite) =>
            ProjetarRelatorio(FiltrarRelatorio(criteria), criteria, limite)
                .AsAsyncEnumerable();

        private IQueryable<Agendamento> FiltrarRelatorio(RelatorioAgendamentoCriteria criteria)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Where(Agendamento.TemReserva)
                .Where(x => x.DataInicio >= criteria.DataInicioUtc
                         && x.DataInicio <= criteria.DataFimUtc);

            if (criteria.HoraInicio.HasValue)
            {
                var horaInicio = criteria.HoraInicio.Value;
                query = query.Where(x =>
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.DataInicio, FusoOperacionalIana).TimeOfDay >= horaInicio);
            }

            if (criteria.HoraFim.HasValue)
            {
                var horaFim = criteria.HoraFim.Value;
                query = query.Where(x =>
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.DataInicio, FusoOperacionalIana).TimeOfDay <= horaFim);
            }

            if (criteria.FornecedorId.HasValue)
            {
                var fornecedorId = criteria.FornecedorId.Value;
                query = query.Where(x =>
                    x.FornecedorId == fornecedorId ||
                    (x.FornecedorId == null && x.NotaFiscal != null && x.NotaFiscal.FornecedorId == fornecedorId));
            }

            if (criteria.UnidadeEntregaId.HasValue)
            {
                var unidadeId = criteria.UnidadeEntregaId.Value;
                query = query.Where(x => x.UnidadeEntregaId == unidadeId);
            }

            if (criteria.LocalDescargaId.HasValue)
            {
                var docaId = criteria.LocalDescargaId.Value;
                query = query.Where(x =>
                    x.LocalDescargaId == docaId ||
                    (x.LocalDescargaId == null && x.Grade != null && x.Grade.LocalDescargaId == docaId));
            }

            if (criteria.Status.Count > 0)
            {
                var status = criteria.Status;
                query = query.Where(x => status.Contains(x.StatusAgendamento));
            }

            if (criteria.Origem == OrigemAgendamento.Grade)
            {
                query = query.Where(x => x.GradeId != null);
            }
            else if (criteria.Origem == OrigemAgendamento.Avulso)
            {
                query = query.Where(x => x.GradeId == null);
            }

            query = criteria.Chegada switch
            {
                SituacaoChegada.Adiantado => query.Where(x => x.ChegouEm != null && x.ChegouEm < x.DataInicio),
                SituacaoChegada.NoHorario => query.Where(x => x.ChegouEm != null && x.ChegouEm >= x.DataInicio && x.ChegouEm <= x.DataFim),
                SituacaoChegada.Atrasado => query.Where(x => x.ChegouEm != null && x.ChegouEm > x.DataFim),
                SituacaoChegada.NaoCompareceu => query.Where(x => x.ChegouEm == null && x.StatusAgendamento == StatusAgendamento.Expirado),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(criteria.Busca))
            {
                var padrao = PadraoContem(criteria.Busca);
                query = query.Where(x =>
                    EF.Functions.ILike(x.PlacaVeiculo!, padrao, "\\") ||
                    (x.Usuario != null && x.Usuario.Motorista != null && EF.Functions.ILike(x.Usuario.Motorista.NomeReal!, padrao, "\\")) ||
                    (x.Fornecedor != null && EF.Functions.ILike(x.Fornecedor.Nome, padrao, "\\")) ||
                    (x.NotaFiscal != null && (
                        EF.Functions.ILike(x.NotaFiscal.PlacaVeiculo!, padrao, "\\") ||
                        EF.Functions.ILike(x.NotaFiscal.Numero.ToString(), padrao, "\\") ||
                        (x.NotaFiscal.Fornecedor != null && EF.Functions.ILike(x.NotaFiscal.Fornecedor.Nome, padrao, "\\")))));
            }

            if (criteria.ProdutoId.HasValue)
            {
                var produtoId = criteria.ProdutoId.Value;
                query = query.Where(x =>
                    x.ProdutoId == produtoId ||
                    (x.Grade != null && x.Grade.ProdutoId == produtoId));
            }

            if (!string.IsNullOrWhiteSpace(criteria.PlacaVeiculo))
            {
                var padrao = PadraoContem(criteria.PlacaVeiculo);
                query = query.Where(x =>
                    EF.Functions.ILike(x.PlacaVeiculo!, padrao, "\\") ||
                    (x.NotaFiscal != null && EF.Functions.ILike(x.NotaFiscal.PlacaVeiculo!, padrao, "\\")));
            }

            if (!string.IsNullOrWhiteSpace(criteria.Motorista))
            {
                var padrao = PadraoContem(criteria.Motorista);
                query = query.Where(x =>
                    x.Usuario != null && x.Usuario.Motorista != null &&
                    EF.Functions.ILike(x.Usuario.Motorista.NomeReal!, padrao, "\\"));
            }

            return query;
        }

        public async Task<PagedResponse<AgendamentoRelatorioLinha>> GetPaginaRelatorioAsync(
            RelatorioAgendamentoCriteria criteria,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var filtrada = FiltrarRelatorio(criteria);
            var total = await filtrada.CountAsync(cancellationToken);

            var itens = await Projetar(Ordenar(filtrada, criteria)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize))
                .ToListAsync(cancellationToken);

            return new PagedResponse<AgendamentoRelatorioLinha>
            {
                Items = itens,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public Task<List<RelatorioResumoLinha>> ListarResumoRelatorioAsync(
            RelatorioAgendamentoCriteria criteria,
            CancellationToken cancellationToken = default) =>
            FiltrarRelatorio(criteria)
                .Select(x => new RelatorioResumoLinha(
                    x.Id,
                    x.StatusAgendamento,
                    x.DataInicio,
                    x.DataFim,
                    x.LocalDescargaId ?? (x.Grade != null ? x.Grade.LocalDescargaId : null),
                    x.ChegouEm,
                    x.InicioDescargaEm,
                    x.FimDescargaEm,
                    x.SaiuEm,
                    (x.NotaFiscal != null ? x.NotaFiscal.PesoBruto : null) ?? x.VolumeCarga ?? 0m))
                .ToListAsync(cancellationToken);

        public async Task<List<FimDescargaDoca>> ListarFinsDescargaAsync(
            DateTime deUtc,
            DateTime ateUtc,
            IReadOnlyCollection<Guid>? docas = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Where(Agendamento.TemReserva)
                .Where(x => x.FimDescargaEm != null && x.FimDescargaEm >= deUtc && x.FimDescargaEm <= ateUtc)
                .Select(x => new
                {
                    Doca = x.LocalDescargaId ?? (x.Grade != null ? x.Grade.LocalDescargaId : null),
                    Fim = x.FimDescargaEm!.Value
                })
                .Where(x => x.Doca != null);

            if (docas is { Count: > 0 })
            {
                query = query.Where(x => docas.Contains(x.Doca!.Value));
            }

            return await query
                .Select(x => new FimDescargaDoca(x.Doca!.Value, x.Fim))
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<Agendamento> Ordenar(IQueryable<Agendamento> query, RelatorioAgendamentoCriteria criteria)
        {
            var desc = criteria.Desc;
            IOrderedQueryable<Agendamento> ordenada = criteria.OrdenarPor switch
            {
                RelatorioOrdenacao.Status => Por(query, x => x.StatusAgendamento, desc),
                RelatorioOrdenacao.Motorista => Por(query, x => x.Usuario != null && x.Usuario.Motorista != null ? x.Usuario.Motorista.NomeReal : null, desc),
                RelatorioOrdenacao.Placa => Por(query, x => x.PlacaVeiculo ?? (x.NotaFiscal != null ? x.NotaFiscal.PlacaVeiculo : null), desc),
                RelatorioOrdenacao.Fornecedor => Por(query, x => x.Fornecedor != null
                    ? x.Fornecedor.Nome
                    : (x.NotaFiscal != null && x.NotaFiscal.Fornecedor != null ? x.NotaFiscal.Fornecedor.Nome : null), desc),
                RelatorioOrdenacao.Produto => Por(query, x => x.Grade != null
                    ? x.Grade.Produto.Nome
                    : (x.Produto != null ? x.Produto.Nome : null), desc),
                RelatorioOrdenacao.Unidade => Por(query, x => x.UnidadeEntrega != null ? x.UnidadeEntrega.Nome : null, desc),
                RelatorioOrdenacao.Doca => Por(query, x => x.LocalDescarga != null
                    ? x.LocalDescarga.Nome
                    : (x.Grade != null && x.Grade.LocalDescarga != null ? x.Grade.LocalDescarga.Nome : null), desc),
                RelatorioOrdenacao.Peso => Por(query, x => (x.NotaFiscal != null ? x.NotaFiscal.PesoBruto : null) ?? x.VolumeCarga, desc),
                RelatorioOrdenacao.Chegada => Por(query, x => x.ChegouEm, desc),
                RelatorioOrdenacao.FimDescarga => Por(query, x => x.FimDescargaEm, desc),
                _ => Por(query, x => x.DataInicio, desc),
            };

            return desc
                ? ordenada.ThenByDescending(x => x.DataInicio).ThenBy(x => x.Id)
                : ordenada.ThenBy(x => x.DataInicio).ThenBy(x => x.Id);
        }

        private static IOrderedQueryable<Agendamento> Por<TChave>(
            IQueryable<Agendamento> query,
            System.Linq.Expressions.Expression<Func<Agendamento, TChave>> chave,
            bool desc)
        {
            var tipo = typeof(TChave);
            if (tipo.IsValueType && Nullable.GetUnderlyingType(tipo) is null)
            {
                return desc ? query.OrderByDescending(chave) : query.OrderBy(chave);
            }

            var vazioPorUltimo = System.Linq.Expressions.Expression.Lambda<Func<Agendamento, bool>>(
                System.Linq.Expressions.Expression.Equal(chave.Body, System.Linq.Expressions.Expression.Constant(null, tipo)),
                chave.Parameters);

            var ordenada = query.OrderBy(vazioPorUltimo);
            return desc ? ordenada.ThenByDescending(chave) : ordenada.ThenBy(chave);
        }

        private static IQueryable<AgendamentoRelatorioLinha> ProjetarRelatorio(
            IQueryable<Agendamento> query,
            RelatorioAgendamentoCriteria criteria,
            int limite) =>
            Projetar(Ordenar(query, criteria).Take(limite));

        private static IQueryable<AgendamentoRelatorioLinha> Projetar(IQueryable<Agendamento> query) =>
            query
                .Select(x => new AgendamentoRelatorioLinha
                {
                    Id = x.Id,
                    LocalDescargaId = x.LocalDescargaId ?? (x.Grade != null ? x.Grade.LocalDescargaId : null),
                    DaGrade = x.GradeId != null,
                    NotaFiscalNumero = x.NotaFiscal != null ? x.NotaFiscal.Numero : null,
                    MotoristaTelefone = x.Usuario != null && x.Usuario.Motorista != null
                        ? x.Usuario.Motorista.Telefone
                        : null,
                    ChegouEm = x.ChegouEm,
                    InicioDescargaEm = x.InicioDescargaEm,
                    FimDescargaEm = x.FimDescargaEm,
                    SaiuEm = x.SaiuEm,
                    FornecedorNome = x.Fornecedor != null
                        ? x.Fornecedor.Nome
                        : (x.NotaFiscal != null && x.NotaFiscal.Fornecedor != null ? x.NotaFiscal.Fornecedor.Nome : null),
                    ProdutoNome = x.Grade != null
                        ? x.Grade.Produto.Nome
                        : (x.Produto != null ? x.Produto.Nome : null),
                    TipoCarga = x.TipoCarga,
                    PlacaVeiculo = x.PlacaVeiculo ?? (x.NotaFiscal != null ? x.NotaFiscal.PlacaVeiculo : null),
                    MotoristaNome = x.Usuario != null && x.Usuario.Motorista != null
                        ? x.Usuario.Motorista.NomeReal
                        : null,
                    UnidadeEntrega = x.UnidadeEntrega != null ? x.UnidadeEntrega.Nome : null,
                    LocalDescarga = x.LocalDescarga != null
                        ? x.LocalDescarga.Nome
                        : (x.Grade != null && x.Grade.LocalDescarga != null ? x.Grade.LocalDescarga.Nome : null),
                    TipoVeiculo = x.TipoVeiculo,
                    PesoCarga = (x.NotaFiscal != null ? x.NotaFiscal.PesoBruto : null) ?? x.VolumeCarga,
                    Status = x.StatusAgendamento,
                    DataInicio = x.DataInicio,
                    DataFim = x.DataFim
                });

        private static string PadraoContem(string termo)
        {
            var escapado = termo.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            return $"%{escapado}%";
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
                    .ThenInclude(n => n!.Fornecedor)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.LocalDescarga)
                .Include(x => x.Empresa)
                .Include(x => x.Grade)
                    .ThenInclude(x => x!.Produto)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return agendamento!;
        }

        public async Task<Agendamento?> GetByIdAcrossTenants(
            Guid id,
            CancellationToken token = default) =>
            await _db.Agendamento
                .IgnoreQueryFilters()
                .Include(x => x.Fornecedor)
                .Include(x => x.Usuario)
                .Include(x => x.NotaFiscal)
                    .ThenInclude(n => n!.Fornecedor)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.LocalDescarga)
                .Include(x => x.Empresa)
                .Include(x => x.Grade)
                    .ThenInclude(x => x!.Produto)
                .FirstOrDefaultAsync(x => x.Id == id, token);

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
                .Include(x => x.LocalDescarga)
                .Include(x => x.Grade).ThenInclude(g => g.Produto)
                .ToListAsync(token);
        }

        public async Task<List<Agendamento>> GetByMotoristaIdAcrossTenants(
            Guid motoristaId,
            CancellationToken token = default) =>
            await _db.Agendamento
                .IgnoreQueryFilters()
                .Where(x => x.Usuario.Motorista.Id == motoristaId)
                .OrderByDescending(x => x.DataInicio)
                .Include(x => x.Fornecedor)
                .Include(x => x.NotaFiscal)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.LocalDescarga)
                .Include(x => x.Grade)
                    .ThenInclude(g => g.Produto)
                .ToListAsync(token);

        public async Task<List<RelatorioMotoristaLinha>> ListarParaRelatorioMotoristaAcrossTenants(
            Guid motoristaId,
            DateTime desdeUtc,
            DateTime ateUtc,
            CancellationToken token = default) =>
            await _db.Agendamento
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Usuario != null && x.Usuario.Motorista != null && x.Usuario.Motorista.Id == motoristaId)
                .Where(x => x.DataInicio >= desdeUtc && x.DataInicio < ateUtc)
                .Select(x => new RelatorioMotoristaLinha(
                    x.Id,
                    x.StatusAgendamento,
                    x.DataInicio,
                    x.NotaFiscal != null ? (x.NotaFiscal.PesoBruto ?? x.VolumeCarga ?? 0m) : (x.VolumeCarga ?? 0m),
                    x.Grade != null ? x.Grade.Produto.Nome : (x.Produto != null ? x.Produto.Nome : null),
                    x.UnidadeEntrega != null ? x.UnidadeEntrega.Nome : null,
                    x.LocalDescarga != null ? x.LocalDescarga.Nome : null,
                    x.Empresa != null ? x.Empresa.NomeFantasia : null))
                .ToListAsync(token);

        public async Task<RelatorioMotoristaProximoDto?> ObterProximoAgendamentoMotoristaAcrossTenants(
            Guid motoristaId,
            DateTime agoraUtc,
            CancellationToken token = default) =>
            await _db.Agendamento
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Usuario != null && x.Usuario.Motorista != null && x.Usuario.Motorista.Id == motoristaId)
                .Where(x => x.StatusAgendamento == StatusAgendamento.Agendado && x.DataFim > agoraUtc)
                .OrderBy(x => x.DataInicio)
                .Select(x => new RelatorioMotoristaProximoDto
                {
                    Id = x.Id,
                    DataInicio = x.DataInicio,
                    UnidadeEntrega = x.UnidadeEntrega != null ? x.UnidadeEntrega.Nome : null,
                    LocalDescarga = x.LocalDescarga != null ? x.LocalDescarga.Nome : null,
                    Empresa = x.Empresa != null ? x.Empresa.NomeFantasia : null,
                })
                .FirstOrDefaultAsync(token);

        public async Task<IReadOnlyList<Agendamento>> ExisteAgendamentoBloqueantePorGrade(
            Guid gradeId,
            CancellationToken cancellationToken = default
            )
        {
            return await _db.Agendamento
                .AsNoTracking()
                .Where(a =>
                    a.GradeId == gradeId &&
                    (a.StatusAgendamento == StatusAgendamento.Pendente ||
                     a.StatusAgendamento == StatusAgendamento.Agendado ||
                     a.StatusAgendamento == StatusAgendamento.EmAndamento))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Agendamento>> ExisteAgendamentoBloqueantePorGradeParaExclusao(
            Guid gradeId,
            CancellationToken cancellationToken = default
            )
        {
            return await _db.Agendamento
                .AsNoTracking()
                .Where(a =>
                    a.GradeId == gradeId &&
                    (a.StatusAgendamento == StatusAgendamento.Pendente ||
                     a.StatusAgendamento == StatusAgendamento.Agendado ||
                     a.StatusAgendamento == StatusAgendamento.EmAndamento ||
                     a.StatusAgendamento == StatusAgendamento.Finalizado))
                .ToListAsync(cancellationToken);
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

        public async Task<List<Agendamento>> GetExpiradosCandidatos(
            DateTime referenciaUtc,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            return await _db.Agendamento
                .Where(x => x.DataFim <= referenciaUtc)
                .Where(x =>
                    x.StatusAgendamento == StatusAgendamento.Disponivel
                    || x.StatusAgendamento == StatusAgendamento.Pendente
                    || x.StatusAgendamento == StatusAgendamento.Agendado)
                .OrderBy(x => x.DataFim)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Agendamento>> GetConflitosAsync(
            Guid localDescargaId,
            DateTime inicioUtc,
            DateTime fimUtc,
            Guid? excludeAgendamentoId,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Agendamento
                .AsNoTracking()
                .Include(x => x.Produto)
                .Include(x => x.LocalDescarga)
                .Where(x => x.LocalDescargaId == localDescargaId)
                .Where(x =>
                    x.StatusAgendamento == StatusAgendamento.Disponivel
                    || x.StatusAgendamento == StatusAgendamento.Pendente
                    || x.StatusAgendamento == StatusAgendamento.Agendado
                    || x.StatusAgendamento == StatusAgendamento.EmAndamento)
                .Where(x => x.DataInicio < fimUtc && inicioUtc < x.DataFim);

            if (excludeAgendamentoId.HasValue)
            {
                query = query.Where(x => x.Id != excludeAgendamentoId.Value);
            }

            return await query.OrderBy(x => x.DataInicio).ToListAsync(cancellationToken);
        }

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            return _db.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task<bool> TryAdvisoryLockEmpresaAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {

            return await _db.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock(hashtext({empresaId.ToString()})) AS \"Value\"")
                .SingleAsync(cancellationToken);
        }

        public async Task DeleteTodosPorGrade(
            Guid gradeId,
            CancellationToken cancellationToken = default
            )
        {
            await _db.Agendamento
                .Where(a => a.GradeId == gradeId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task DeleteDisponiveisPorGrade(
            Guid gradeId,
            CancellationToken cancellationToken = default
            )
        {
            await _db.Agendamento
                .Where(a => a.GradeId == gradeId && a.StatusAgendamento == StatusAgendamento.Disponivel)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
