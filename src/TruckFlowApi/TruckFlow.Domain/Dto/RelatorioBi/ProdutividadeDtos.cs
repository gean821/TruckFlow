namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class ProdutividadeFiltroDto
    {
        public DateOnly? Inicio { get; set; }
        public DateOnly? Fim { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
        public int? LimiteEsperaMin { get; set; }

        public RelatorioBiFiltroDto ParaFiltroBi() => new()
        {
            Inicio = Inicio,
            Fim = Fim,
            UnidadeEntregaId = UnidadeEntregaId,
            LocalDescargaId = LocalDescargaId,
            FornecedorId = FornecedorId,
            ProdutoId = ProdutoId,
        };
    }

    public sealed class ProdutividadeResponseDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public int LimiteEsperaMin { get; set; }
        public ProdutividadeIndicadoresDto Indicadores { get; set; } = new();
        public List<ProdutividadeDiaDto> SerieDiaria { get; set; } = [];
        public List<ProdutividadeFaixaDto> Distribuicao { get; set; } = [];
        public List<ProdutividadeCelulaDto> Heatmap { get; set; } = [];
        public List<ProdutividadeHoraDto> PorHora { get; set; } = [];
        public List<ProdutividadeGrupoDto> PorDoca { get; set; } = [];
        public List<ProdutividadeGrupoDto> PorProduto { get; set; } = [];
        public List<ProdutividadeGrupoDto> PorUnidade { get; set; } = [];
        public List<ProdutividadeGrupoDto> PorFornecedor { get; set; } = [];
        public List<VisaoGeralDestaqueDto> Destaques { get; set; } = [];
    }

    public sealed class ProdutividadeIndicadoresDto
    {
        public RelatorioBiIndicadorDto Ciclos { get; set; } = new();
        public RelatorioBiIndicadorDto EsperaMediaMin { get; set; } = new();
        public RelatorioBiIndicadorDto EsperaP90Min { get; set; } = new();
        public RelatorioBiIndicadorDto DescargaMediaMin { get; set; } = new();
        public RelatorioBiIndicadorDto PermanenciaMediaMin { get; set; } = new();
        public RelatorioBiIndicadorDto HorasEspera { get; set; } = new();
        public RelatorioBiIndicadorDto AcimaDoLimitePct { get; set; } = new();
        public decimal? EsperaMedianaMin { get; set; }
        public decimal? DescargaMedianaMin { get; set; }
        public decimal? DescargaP90Min { get; set; }
        public decimal? PermanenciaP90Min { get; set; }
        public int AcimaDoLimite { get; set; }
        public decimal DescargasPorDia { get; set; }
        public decimal EstimadosPct { get; set; }
    }

    public sealed class ProdutividadeDiaDto
    {
        public DateOnly Dia { get; set; }
        public int Ciclos { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? EsperaP90Min { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public decimal? PermanenciaMediaMin { get; set; }
    }

    public sealed class ProdutividadeFaixaDto
    {
        public string Rotulo { get; set; } = string.Empty;
        public int DeMin { get; set; }
        public int? AteMin { get; set; }
        public int Espera { get; set; }
        public int Descarga { get; set; }
    }

    public sealed class ProdutividadeCelulaDto
    {
        public int DiaSemana { get; set; }
        public int Hora { get; set; }
        public int Chegadas { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? EsperaP90Min { get; set; }
    }

    public sealed class ProdutividadeHoraDto
    {
        public int Hora { get; set; }
        public int Chegadas { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? DescargaMediaMin { get; set; }
    }

    public sealed class ProdutividadeCaixaDto
    {
        public decimal Min { get; set; }
        public decimal Q1 { get; set; }
        public decimal Mediana { get; set; }
        public decimal Q3 { get; set; }
        public decimal Max { get; set; }
    }

    public sealed class ProdutividadeGrupoDto
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ciclos { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? EsperaP90Min { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public decimal? DescargaMedianaMin { get; set; }
        public decimal? PermanenciaMediaMin { get; set; }
        public int AcimaDoLimite { get; set; }
        public ProdutividadeCaixaDto? CaixaEspera { get; set; }
        public ProdutividadeCaixaDto? CaixaDescarga { get; set; }
    }

    public sealed class ProdutividadePatioFiltroDto
    {
        public DateOnly? Dia { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
    }

    public sealed class ProdutividadePatioDto
    {
        public DateOnly Dia { get; set; }
        public DateTime? AgoraUtc { get; set; }
        public DateTime? InicioEixo { get; set; }
        public DateTime? FimEixo { get; set; }
        public int Caminhoes { get; set; }
        public decimal? EsperaMediaMin { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public int PicoSimultaneos { get; set; }
        public DateTime? PicoEm { get; set; }
        public List<ProdutividadePatioDocaDto> Docas { get; set; } = [];
    }

    public sealed class ProdutividadePatioDocaDto
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Unidade { get; set; }
        public decimal? DescargaMediaMin { get; set; }
        public List<ProdutividadePatioCaminhaoDto> Caminhoes { get; set; } = [];
    }

    public sealed class ProdutividadePatioCaminhaoDto
    {
        public Guid AgendamentoId { get; set; }
        public string? Placa { get; set; }
        public string? Motorista { get; set; }
        public string? Fornecedor { get; set; }
        public string? Produto { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime JanelaInicio { get; set; }
        public DateTime JanelaFim { get; set; }
        public DateTime ChegouEm { get; set; }
        public DateTime? InicioDescarga { get; set; }
        public bool InicioEstimado { get; set; }
        public DateTime? FimDescarga { get; set; }
        public DateTime? Saida { get; set; }
        public bool SaidaEstimada { get; set; }
        public decimal? EsperaMin { get; set; }
        public decimal? DescargaMin { get; set; }
    }
}
