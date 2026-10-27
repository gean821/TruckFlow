namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class VisaoGeralResponseDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public VisaoGeralIndicadoresDto Indicadores { get; set; } = new();
        public List<VisaoGeralDiaDto> SerieDiaria { get; set; } = [];
        public List<VisaoGeralDiaDto> SerieDiariaAnterior { get; set; } = [];
        public decimal? MetaDiariaKg { get; set; }
        public VisaoGeralStatusDto PorStatus { get; set; } = new();
        public VisaoGeralFunilDto Funil { get; set; } = new();
        public List<List<int>> HeatmapDiaHora { get; set; } = [];
        public List<VisaoGeralOcupacaoHoraDto> OcupacaoPorHora { get; set; } = [];
        public int DocasAtivas { get; set; }
        public List<RelatorioBiRankingDto> TopFornecedores { get; set; } = [];
        public List<RelatorioBiRankingDto> TopProdutos { get; set; } = [];
        public List<VisaoGeralDestaqueDto> Destaques { get; set; } = [];
    }

    public sealed class VisaoGeralIndicadoresDto
    {
        public RelatorioBiIndicadorDto Agendamentos { get; set; } = new();
        public RelatorioBiIndicadorDto Concluidos { get; set; } = new();
        public RelatorioBiIndicadorDto PesoRecebidoKg { get; set; } = new();
        public RelatorioBiIndicadorDto ComparecimentoPct { get; set; } = new();
        public RelatorioBiIndicadorDto Cancelados { get; set; } = new();
        public RelatorioBiIndicadorDto NaoCompareceu { get; set; } = new();
        public RelatorioBiIndicadorDto PesoMedioKg { get; set; } = new();
        public decimal? AtingimentoMetaPct { get; set; }
    }

    public sealed class VisaoGeralDiaDto
    {
        public DateOnly Data { get; set; }
        public int Agendamentos { get; set; }
        public int Concluidos { get; set; }
        public int Cancelados { get; set; }
        public int NaoCompareceu { get; set; }
        public decimal PesoKg { get; set; }
    }

    public sealed class VisaoGeralStatusDto
    {
        public int Agendado { get; set; }
        public int EmAndamento { get; set; }
        public int Finalizado { get; set; }
        public int Cancelado { get; set; }
        public int Expirado { get; set; }
    }

    public sealed class VisaoGeralFunilDto
    {
        public int Reservados { get; set; }
        public int Chegaram { get; set; }
        public int Concluidos { get; set; }
    }

    public sealed class VisaoGeralOcupacaoHoraDto
    {
        public int Hora { get; set; }
        public decimal OcupacaoPct { get; set; }
        public decimal Agendamentos { get; set; }
    }

    public sealed class VisaoGeralDestaqueDto
    {
        public string Tipo { get; set; } = "info";
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }
}
