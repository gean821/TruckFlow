namespace TruckFlow.Domain.Dto.Dashboard
{
    public sealed class DashboardHojeResponseDto
    {
        public DateTime AgoraUtc { get; set; }
        public DateOnly Dia { get; set; }
        public int HoraAtual { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public DashboardHojeIndicadoresDto Indicadores { get; set; } = new();
        public decimal? MetaDiariaKg { get; set; }
        public List<DashboardHojeHoraDto> PorHora { get; set; } = [];
        public List<DashboardHojeDocaDto> Docas { get; set; } = [];
        public DashboardHojePatioDto Patio { get; set; } = new();
        public List<DashboardHojeCaminhaoDto> ProximasChegadas { get; set; } = [];
        public List<DashboardHojeCaminhaoDto> Atrasados { get; set; } = [];
    }

    public sealed class DashboardHojeIndicadorDto
    {
        public decimal Valor { get; set; }
        public decimal? Referencia { get; set; }
        public decimal? VariacaoPct { get; set; }
    }

    public sealed class DashboardHojeIndicadoresDto
    {
        public DashboardHojeIndicadorDto AgendadosHoje { get; set; } = new();
        public DashboardHojeIndicadorDto Concluidos { get; set; } = new();
        public DashboardHojeIndicadorDto NoPatio { get; set; } = new();
        public DashboardHojeIndicadorDto Atrasados { get; set; } = new();
        public DashboardHojeIndicadorDto NaoCompareceu { get; set; } = new();
        public DashboardHojeIndicadorDto PesoRecebidoKg { get; set; } = new();
        public int AindaPorVir { get; set; }
        public decimal? AtingimentoMetaPct { get; set; }
    }

    public sealed class DashboardHojeHoraDto
    {
        public int Hora { get; set; }
        public int Agendados { get; set; }
        public int Chegadas { get; set; }
        public int Concluidos { get; set; }
        public decimal MediaAgendados { get; set; }
    }

    public sealed class DashboardHojeDocaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Unidade { get; set; }
        public bool Ocupada { get; set; }
        public DashboardHojeCaminhaoDto? Atual { get; set; }
        public DashboardHojeCaminhaoDto? Proximo { get; set; }
        public int ConcluidosHoje { get; set; }
    }

    public sealed class DashboardHojePatioDto
    {
        public int NoPatio { get; set; }
        public double? PermanenciaMediaMin { get; set; }
        public double? DescargaMediaMin { get; set; }
        public double? EsperaMediaMin { get; set; }
        public DashboardHojeCaminhaoDto? MaisAntigo { get; set; }
    }

    public sealed class DashboardHojeCaminhaoDto
    {
        public Guid AgendamentoId { get; set; }
        public DateTime DataInicio { get; set; }
        public string? Placa { get; set; }
        public string? Motorista { get; set; }
        public string? Fornecedor { get; set; }
        public string? Produto { get; set; }
        public string? Doca { get; set; }
        public decimal PesoKg { get; set; }
        public int? Minutos { get; set; }
    }
}
