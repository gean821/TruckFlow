using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Dashboard
{
    public class DashboardResponseDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public DashboardVolumeDto Volume { get; set; } = new();
        public DashboardDocasDto Docas { get; set; } = new();
        public List<DashboardActivityDto> RecentActivity { get; set; } = new();
    }

    public class DashboardStatsDto
    {
        public int TotalAgendamentos { get; set; }
        public int EmAndamento { get; set; }
        public int Finalizados { get; set; }
        public int Atrasados { get; set; }
    }

    public class DashboardVolumeDto
    {
        public decimal TotalKg { get; set; }
        public int ProgressoDiario { get; set; }
    }

    public class DashboardDocasDto
    {
        public int OcupacaoPorcentagem { get; set; }
        public int Livres { get; set; }
        public int Ocupadas { get; set; }
        public int Total { get; set; }
    }

    public class DashboardActivityDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;   
        public string Subtitle { get; set; } = string.Empty; // Ex: "Mot. João (ABC-1234)"
        public string Time { get; set; } = string.Empty;     // Ex: "10:30"
        public string Type { get; set; } = string.Empty;     // "checkin", "checkout", "schedule", "delay"
        public string Color { get; set; } = string.Empty;    // "info", "success", "primary", "error"
        public string Icon { get; set; } = string.Empty;     // "mdi-truck", etc
    }
}
