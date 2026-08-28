using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Relatorio;
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
                    FornecedorNome = x.Fornecedor.Nome,
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

        public async Task<List<AgendamentoAdminResponse>> GetForRelatorioAsync(
            RelatorioAgendamentoFilterDto filtros,
            CancellationToken cancellationToken)
        {
            const int maxLinhas = 20_000;

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
                .AsQueryable();

            if (filtros.DataInicio.HasValue)
            {
                query = query.Where(x => x.DataInicio >= filtros.DataInicio.Value);
            }

            if (filtros.FornecedorId.HasValue)
            {
                query = query.Where(x => x.FornecedorId == filtros.FornecedorId.Value);
            }

            if (filtros.ProdutoId.HasValue)
            {
                var produtoId = filtros.ProdutoId.Value;
                query = query.Where(x =>
                    x.ProdutoId == produtoId ||
                    (x.Grade != null && x.Grade.ProdutoId == produtoId));
            }

            if (!string.IsNullOrWhiteSpace(filtros.Motorista))
            {
                var nome = filtros.Motorista.Trim();
                query = query.Where(x =>
                    x.Usuario != null && x.Usuario.Motorista != null &&
                    x.Usuario.Motorista.NomeReal.Contains(nome));
            }

            return await query
                .OrderBy(x => x.DataInicio)
                .Take(maxLinhas)
                .Select(x => new AgendamentoAdminResponse
                {
                    Id = x.Id,
                    FornecedorNome = x.Fornecedor.Nome,
                    MotoristaNome = x.Usuario != null
                                     ? x.Usuario.Motorista != null ? x.Usuario.Motorista.NomeReal : null
                                     : null,
                    MotoristaTelefone = x.Usuario != null
                                ? x.Usuario.Motorista != null ? x.Usuario.Motorista.Telefone : null
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
                    UpdatedAt = x.UpdatedAt,
                    DeletedAt = x.DeletedAt
                })
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
                .Include(x => x.LocalDescarga)
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
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.LocalDescarga)
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
