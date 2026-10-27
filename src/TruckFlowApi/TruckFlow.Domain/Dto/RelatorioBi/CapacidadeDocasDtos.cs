using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed record CapacidadeVagaLinha(
        Guid Id,
        StatusAgendamento Status,
        DateTime DataInicio,
        DateTime DataFim,
        bool TemReserva,
        bool DaGrade,
        Guid? LocalDescargaId,
        string? LocalDescargaNome,
        string? UnidadeEntregaNome,
        DateTime? ChegouEm,
        DateTime? InicioDescargaEm,
        DateTime? FimDescargaEm);

    public sealed class CapacidadeDocasResponseDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public CapacidadeIndicadoresDto Indicadores { get; set; } = new();
        public List<CapacidadeDiaDto> SerieDiaria { get; set; } = [];
        public List<CapacidadeCelulaDto> HeatmapSemana { get; set; } = [];
        public List<CapacidadeDocaHoraDto> HeatmapDocas { get; set; } = [];
        public List<CapacidadeDocaDto> Docas { get; set; } = [];
        public List<CapacidadeJanelaDto> ExcessoDemanda { get; set; } = [];
        public List<CapacidadeJanelaDto> Ociosas { get; set; } = [];
        public List<CapacidadeHoraDto> PorHora { get; set; } = [];
        public List<VisaoGeralDestaqueDto> Recomendacoes { get; set; } = [];
    }

    public sealed class CapacidadeIndicadoresDto
    {
        public RelatorioBiIndicadorDto TaxaReservaPct { get; set; } = new();
        public RelatorioBiIndicadorDto UtilizacaoRealPct { get; set; } = new();
        public RelatorioBiIndicadorDto HorasOfertadas { get; set; } = new();
        public RelatorioBiIndicadorDto HorasOciosas { get; set; } = new();
        public RelatorioBiIndicadorDto HorasNaoComparecimento { get; set; } = new();
        public RelatorioBiIndicadorDto ForaDaGrade { get; set; } = new();
        public int VagasOfertadas { get; set; }
        public int VagasReservadas { get; set; }
        public int JanelasEsgotadas { get; set; }
        public int JanelasComOferta { get; set; }
        public decimal? VagaMediaMin { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public decimal? AproveitamentoVagaPct { get; set; }
    }

    public sealed class CapacidadeDiaDto
    {
        public DateOnly Data { get; set; }
        public decimal HorasOfertadas { get; set; }
        public decimal HorasReservadas { get; set; }
        public decimal HorasUtilizadas { get; set; }
        public decimal HorasNaoComparecimento { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public decimal? UtilizacaoRealPct { get; set; }
    }

    public sealed class CapacidadeCelulaDto
    {
        public int DiaSemana { get; set; }
        public int Hora { get; set; }
        public int Ofertadas { get; set; }
        public int Reservadas { get; set; }
        public int Realizadas { get; set; }
        public int NaoCompareceu { get; set; }
        public int ForaDaGrade { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public decimal? UtilizacaoRealPct { get; set; }
        public int DiasComOferta { get; set; }
        public int DiasEsgotados { get; set; }
    }

    public sealed class CapacidadeDocaHoraDto
    {
        public Guid DocaId { get; set; }
        public int Hora { get; set; }
        public int Ofertadas { get; set; }
        public int Reservadas { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public decimal? UtilizacaoRealPct { get; set; }
    }

    public sealed class CapacidadeDocaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Unidade { get; set; }
        public int VagasOfertadas { get; set; }
        public int VagasReservadas { get; set; }
        public int Descargas { get; set; }
        public int NaoCompareceu { get; set; }
        public int ForaDaGrade { get; set; }
        public decimal HorasOfertadas { get; set; }
        public decimal HorasReservadas { get; set; }
        public decimal HorasUtilizadas { get; set; }
        public decimal HorasOciosas { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public decimal? UtilizacaoRealPct { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public int? HoraPico { get; set; }
        public List<decimal?> SerieTaxaReserva { get; set; } = [];
    }

    public sealed class CapacidadeJanelaDto
    {
        public int DiaSemana { get; set; }
        public int Hora { get; set; }
        public string Rotulo { get; set; } = string.Empty;
        public int Ofertadas { get; set; }
        public int Reservadas { get; set; }
        public int ForaDaGrade { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public int DiasComOferta { get; set; }
        public int DiasEsgotados { get; set; }
        public int VagasLivres { get; set; }
    }

    public sealed class CapacidadeHoraDto
    {
        public int Hora { get; set; }
        public decimal HorasOfertadas { get; set; }
        public decimal HorasReservadas { get; set; }
        public decimal HorasUtilizadas { get; set; }
        public decimal? TaxaReservaPct { get; set; }
        public decimal? UtilizacaoRealPct { get; set; }
    }
}
