using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class DashboardRepositorio : IDashboardRepositorio
    {
        private readonly AppDbContext _db;
        public DashboardRepositorio(AppDbContext db) => _db = db;

        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(
            DateTime inicioDiaUtc,
            DateTime fimDiaUtc,
            DateTime agoraUtc,
            CancellationToken token = default)
        {
            var doDia = _db.Agendamento
                .Where(x => x.DataInicio >= inicioDiaUtc && x.DataInicio < fimDiaUtc);

            var totalAgendamentosHoje = await doDia
                .CountAsync(x => x.StatusAgendamento != StatusAgendamento.Disponivel, token);

            var emAndamento = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.EmAndamento, token);

            var finalizadosHoje = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Finalizado
                    && x.UpdatedAt >= inicioDiaUtc && x.UpdatedAt < fimDiaUtc, token);

            var atrasados = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Agendado && x.DataInicio < agoraUtc, token);

            var canceladosHoje = await doDia
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Cancelado, token);

            var expiradosHoje = await doDia
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Expirado, token);

            var volumeTotal = await doDia
                .Where(x => x.StatusAgendamento != StatusAgendamento.Cancelado)
                .SumAsync(x => x.NotaFiscal != null ? (x.NotaFiscal.PesoBruto ?? 0) : (x.VolumeCarga ?? 0), token);

            var docasAtivas = _db.LocalDescarga.Where(l => l.Ativa != false);

            var totalDocas = await docasAtivas.CountAsync(token);

            var docasOcupadas = await _db.Agendamento
                .Where(x => x.StatusAgendamento == StatusAgendamento.EmAndamento)
                .Select(x => x.LocalDescargaId ?? x.Grade!.LocalDescargaId)
                .Where(id => id != null && docasAtivas.Any(l => l.Id == id))
                .Distinct()
                .CountAsync(token);

            var ultimasAtividades = await _db.Agendamento
                .AsNoTracking()
                .Include(x => x.Usuario).ThenInclude(u => u.Motorista)
                .Include(x => x.Fornecedor)
                .Include(x => x.UnidadeEntrega)
                .Include(x => x.NotaFiscal)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Take(5)
                .ToListAsync(token);

            var response = new DashboardResponseDto
            {
                Stats = new DashboardStatsDto
                {
                    TotalAgendamentos = totalAgendamentosHoje,
                    EmAndamento = emAndamento,
                    Finalizados = finalizadosHoje,
                    Atrasados = atrasados,
                    Cancelados = canceladosHoje,
                    Expirados = expiradosHoje
                },
                Volume = new DashboardVolumeDto
                {
                    TotalKg = volumeTotal,
                    ProgressoDiario = 0
                },
                Docas = new DashboardDocasDto
                {
                    Total = totalDocas,
                    Ocupadas = docasOcupadas,
                    Livres = Math.Max(0, totalDocas - docasOcupadas),
                    OcupacaoPorcentagem = totalDocas > 0 ? (int)((double)docasOcupadas / totalDocas * 100) : 0
                }
            };

            response.RecentActivity = ultimasAtividades.Select(a => new DashboardActivityDto
            {
                Id = a.Id,
                Type = GetActivityType(a.StatusAgendamento, a.DataInicio),
                Title = GetTitle(a.StatusAgendamento),
                Subtitle = $"{a.Usuario?.Motorista?.NomeReal ?? "Motorista"} - {a.Fornecedor?.Nome}",
                Time = TimeZoneInfo
                    .ConvertTimeFromUtc(DateTime.SpecifyKind(a.UpdatedAt ?? a.CreatedAt, DateTimeKind.Utc), Grade.OperationalTimeZone)
                    .ToString("HH:mm"),
                Color = GetColor(a.StatusAgendamento, a.DataInicio),
                Icon = GetIcon(a.StatusAgendamento)
            }).ToList();

            return response;
        }
        private string GetActivityType(StatusAgendamento status, DateTime inicio)
        {
            if (status == StatusAgendamento.Agendado && inicio < DateTime.UtcNow) return "delay";
            return status switch
            {
                StatusAgendamento.EmAndamento => "checkin",
                StatusAgendamento.Finalizado => "checkout",
                StatusAgendamento.Expirado => "expired",
                StatusAgendamento.Cancelado => "cancel",
                _ => "schedule"
            };
        }

        private string GetTitle(StatusAgendamento status) => status switch
        {
            StatusAgendamento.EmAndamento => "Entrada Registrada",
            StatusAgendamento.Finalizado => "Descarga Concluída",
            StatusAgendamento.Agendado => "Agendamento Criado",
            StatusAgendamento.Expirado => "Agendamento Expirado",
            StatusAgendamento.Cancelado => "Agendamento Cancelado",
            _ => "Atualização"
        };

        private string GetColor(StatusAgendamento status, DateTime inicio)
        {
            if (status == StatusAgendamento.Agendado && inicio < DateTime.UtcNow) return "error";
            return status switch
            {
                StatusAgendamento.EmAndamento => "info",
                StatusAgendamento.Finalizado => "success",
                StatusAgendamento.Expirado => "warning",
                StatusAgendamento.Cancelado => "error",
                _ => "primary"
            };
        }
        private string GetIcon(StatusAgendamento status) => status switch
        {
            StatusAgendamento.EmAndamento => "mdi-truck-check",
            StatusAgendamento.Finalizado => "mdi-check-all",
            StatusAgendamento.Expirado => "mdi-clock-alert-outline",
            StatusAgendamento.Cancelado => "mdi-cancel",
            _ => "mdi-calendar-clock"
        };
    }
}

