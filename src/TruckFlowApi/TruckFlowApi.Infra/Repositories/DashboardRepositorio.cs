using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class DashboardRepositorio : IDashboardRepositorio
    {
        private readonly AppDbContext _db;
        public DashboardRepositorio(AppDbContext db) => _db = db;

        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(CancellationToken token = default)
        {
            var hoje = DateTime.UtcNow.Date;
            var agora = DateTime.UtcNow;

            // 1. Buscando dados agregados (Counts e Sums)
            // DICA: Fazer queries separadas é mais eficiente do que trazer tudo para a memória

            var totalAgendamentosHoje = await _db.Agendamento
                .CountAsync(x => x.DataInicio.Date == hoje, token);

            var emAndamento = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.EmAndamento, token);

            var finalizadosHoje = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Finalizado && x.UpdatedAt >= hoje, token);

            var atrasados = await _db.Agendamento
                .CountAsync(x => x.StatusAgendamento == StatusAgendamento.Agendado && x.DataInicio < agora, token);

            // Soma do volume (Prioriza PesoBruto da NF, se não tiver, usa VolumeCarga estimado)
            var volumeTotal = await _db.Agendamento
                .Where(x => x.DataInicio.Date == hoje && x.StatusAgendamento != StatusAgendamento.Cancelado)
                .SumAsync(x => x.NotaFiscal != null ? (x.NotaFiscal.PesoBruto ?? 0) : (x.VolumeCarga ?? 0), token);

            // Dados de Docas
            var totalDocas = await _db.UnidadeEntrega.CountAsync(token);
            // Assumimos que cada agendamento em andamento ocupa uma doca (simplificação)
            var docasOcupadas = emAndamento;

            // 2. Buscando Atividades Recentes (Os últimos 5 eventos)
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
                    Atrasados = atrasados
                },
                Volume = new DashboardVolumeDto
                {
                    TotalKg = volumeTotal,
                    // A lógica de % da meta pode ficar aqui ou no Service. 
                    // Vou deixar 0 aqui para o Service calcular se houver regra de negócio complexa.
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

            // Mapeamento das atividades para o DTO
            response.RecentActivity = ultimasAtividades.Select(a => new DashboardActivityDto
            {
                Id = a.Id,
                // Lógica simples de mapeamento
                Type = GetActivityType(a.StatusAgendamento, a.DataInicio),
                Title = GetTitle(a.StatusAgendamento),
                Subtitle = $"{a.Usuario?.Motorista?.NomeReal ?? "Motorista"} - {a.Fornecedor.Nome}",
                Time = (a.UpdatedAt ?? a.CreatedAt).ToLocalTime().ToString("HH:mm"),
                Color = GetColor(a.StatusAgendamento, a.DataInicio),
                Icon = GetIcon(a.StatusAgendamento)
            }).ToList();

            return response;
        }

        // Helpers privados para mapear Enums para Strings do Front
        // (Isso poderia estar no Service, mas como o Repo retorna o DTO pronto, pode ficar aqui para simplificar a query)
        private string GetActivityType(StatusAgendamento status, DateTime inicio)
        {
            if (status == StatusAgendamento.Agendado && inicio < DateTime.UtcNow) return "delay";
            return status switch
            {
                StatusAgendamento.EmAndamento => "checkin",
                StatusAgendamento.Finalizado => "checkout",
                _ => "schedule"
            };
        }

        private string GetTitle(StatusAgendamento status) => status switch
        {
            StatusAgendamento.EmAndamento => "Entrada Registrada",
            StatusAgendamento.Finalizado => "Descarga Concluída",
            StatusAgendamento.Agendado => "Agendamento Criado",
            _ => "Atualização"
        };

        private string GetColor(StatusAgendamento status, DateTime inicio)
        {
            if (status == StatusAgendamento.Agendado && inicio < DateTime.UtcNow) return "error";
            return status switch
            {
                StatusAgendamento.EmAndamento => "info",
                StatusAgendamento.Finalizado => "success",
                _ => "primary"
            };
        }
        private string GetIcon(StatusAgendamento status) => status switch
        {
            StatusAgendamento.EmAndamento => "mdi-truck-check",
            StatusAgendamento.Finalizado => "mdi-check-all",
            _ => "mdi-calendar-clock"
        };
    }
}

