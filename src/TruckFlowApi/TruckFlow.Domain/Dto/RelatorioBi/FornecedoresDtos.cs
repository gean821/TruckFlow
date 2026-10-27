namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class FornecedoresResponseDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public string Agrupamento { get; set; } = "dia";
        public FornecedoresIndicadoresDto Indicadores { get; set; } = new();
        public List<FornecedoresBaldeDto> Serie { get; set; } = [];
        public List<FornecedorDesempenhoDto> Ranking { get; set; } = [];
        public List<VisaoGeralDestaqueDto> Destaques { get; set; } = [];
    }

    public sealed class FornecedoresIndicadoresDto
    {
        public RelatorioBiIndicadorDto FornecedoresAtivos { get; set; } = new();
        public RelatorioBiIndicadorDto PontualidadePct { get; set; } = new();
        public RelatorioBiIndicadorDto NoShowPct { get; set; } = new();
        public RelatorioBiIndicadorDto CancelamentoPct { get; set; } = new();
        public RelatorioBiIndicadorDto AtrasoMedioMin { get; set; } = new();
        public RelatorioBiIndicadorDto PesoRecebidoKg { get; set; } = new();
        public decimal? ScoreMedio { get; set; }
        public decimal? ConcentracaoTop3Pct { get; set; }
        public int ComScoreCritico { get; set; }
    }

    public sealed class FornecedoresBaldeDto
    {
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public string Rotulo { get; set; } = string.Empty;
        public int Agendamentos { get; set; }
        public decimal PesoKg { get; set; }
        public decimal? PontualidadePct { get; set; }
        public decimal? NoShowPct { get; set; }
        public decimal? CancelamentoPct { get; set; }
    }

    public sealed class FornecedorDesempenhoDto
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Agendamentos { get; set; }
        public int Realizaveis { get; set; }
        public int Concluidos { get; set; }
        public decimal PesoKg { get; set; }
        public decimal PctVolume { get; set; }
        public int Chegaram { get; set; }
        public int Adiantados { get; set; }
        public int NoHorario { get; set; }
        public int Atrasados { get; set; }
        public decimal? PontualidadePct { get; set; }
        public decimal? PontualidadeAnteriorPct { get; set; }
        public decimal? VariacaoPontualidade { get; set; }
        public decimal? AtrasoMedioMin { get; set; }
        public int NaoCompareceu { get; set; }
        public decimal? NoShowPct { get; set; }
        public int Cancelados { get; set; }
        public decimal? CancelamentoPct { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public decimal? Score { get; set; }
        public string Classificacao { get; set; } = string.Empty;
        public string? ProdutoPrincipal { get; set; }
        public List<decimal?> SeriePontualidade { get; set; } = [];
    }

    public sealed class FornecedorDetalheDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public string Agrupamento { get; set; } = "dia";
        public FornecedorDesempenhoDto Fornecedor { get; set; } = new();
        public FornecedorDesempenhoDto Media { get; set; } = new();
        public int Posicao { get; set; }
        public int TotalFornecedores { get; set; }
        public List<FornecedoresBaldeDto> Serie { get; set; } = [];
        public List<FornecedorGrupoDto> PorProduto { get; set; } = [];
        public List<FornecedorGrupoDto> PorUnidade { get; set; } = [];
        public List<FornecedorGrupoDto> PorDiaSemana { get; set; } = [];
        public List<FornecedorFaixaChegadaDto> DistribuicaoChegada { get; set; } = [];
        public List<FornecedorOcorrenciaDto> Ocorrencias { get; set; } = [];
    }

    public sealed class FornecedorGrupoDto
    {
        public string Nome { get; set; } = string.Empty;
        public int Agendamentos { get; set; }
        public decimal PesoKg { get; set; }
        public decimal? PontualidadePct { get; set; }
        public decimal? NoShowPct { get; set; }
        public decimal? CancelamentoPct { get; set; }
    }

    public sealed class FornecedorFaixaChegadaDto
    {
        public string Rotulo { get; set; } = string.Empty;
        public int? DeMin { get; set; }
        public int? AteMin { get; set; }
        public int Quantidade { get; set; }
        public bool Atrasado { get; set; }
    }

    public sealed class FornecedorOcorrenciaDto
    {
        public Guid AgendamentoId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal? AtrasoMin { get; set; }
        public string? Placa { get; set; }
        public string? Motorista { get; set; }
        public string? Produto { get; set; }
        public string? Doca { get; set; }
    }
}
