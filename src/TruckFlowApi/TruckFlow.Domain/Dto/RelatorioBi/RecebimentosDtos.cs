namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class RecebimentosFiltroDto
    {
        public DateOnly? Inicio { get; set; }
        public DateOnly? Fim { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? Agrupamento { get; set; }

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

    public sealed record RecebimentoLinha(
        Guid AgendamentoId,
        DateTime DataInicio,
        decimal PesoNotaKg,
        decimal? PesoRecebidoKg,
        Guid? ProdutoId,
        string? ProdutoNome,
        Guid? FornecedorId,
        string? FornecedorNome,
        Guid? UnidadeEntregaId,
        string? UnidadeEntregaNome,
        string? PlacaVeiculo,
        long? NotaNumero);

    public sealed record PlanejamentoItemLinha(
        Guid ItemId,
        Guid ProdutoId,
        string ProdutoNome,
        Guid FornecedorId,
        string FornecedorNome,
        DateTime PlanejamentoInicio,
        DateTime PlanejamentoFim,
        decimal CadenciaDiariaKg,
        string DiasSemana,
        decimal QuantidadeTotalPlanejadaKg,
        decimal RecebidoNoPeriodoKg);

    public sealed class RecebimentosResponseDto
    {
        public RelatorioBiPeriodoDto Periodo { get; set; } = new();
        public string Agrupamento { get; set; } = "dia";
        public RecebimentosIndicadoresDto Indicadores { get; set; } = new();
        public List<string> Produtos { get; set; } = [];
        public List<RecebimentosBaldeDto> Serie { get; set; } = [];
        public List<RelatorioBiRankingDto> PorProduto { get; set; } = [];
        public List<RelatorioBiRankingDto> PorFornecedor { get; set; } = [];
        public List<RelatorioBiRankingDto> PorUnidade { get; set; } = [];
        public RecebimentosFluxoDto Fluxo { get; set; } = new();
        public List<RecebimentosPlanejamentoDto> Planejamento { get; set; } = [];
        public List<RecebimentosDivergenciaDto> Divergencias { get; set; } = [];
        public RecebimentosMatrizDto Matriz { get; set; } = new();
    }

    public sealed class RecebimentosIndicadoresDto
    {
        public RelatorioBiIndicadorDto PesoNotaKg { get; set; } = new();
        public RelatorioBiIndicadorDto Descargas { get; set; } = new();
        public RelatorioBiIndicadorDto PesoMedioKg { get; set; } = new();
        public decimal? PesoRecebidoKg { get; set; }
        public decimal? PesoNotaConferidoKg { get; set; }
        public decimal? DivergenciaKg { get; set; }
        public decimal? DivergenciaPct { get; set; }
        public decimal CoberturaConferenciaPct { get; set; }
        public decimal? CumprimentoPlanejamentoPct { get; set; }
        public int Orfaos { get; set; }
        public decimal OrfaosKg { get; set; }
        public decimal? MetaPeriodoKg { get; set; }
    }

    public sealed class RecebimentosBaldeDto
    {
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public string Rotulo { get; set; } = string.Empty;
        public decimal TotalKg { get; set; }
        public int Descargas { get; set; }
        public Dictionary<string, decimal> PorProduto { get; set; } = [];
        public decimal? MetaKg { get; set; }
    }

    public sealed class RecebimentosFluxoDto
    {
        public List<RecebimentosFluxoNoDto> Nos { get; set; } = [];
        public List<RecebimentosFluxoLigacaoDto> Ligacoes { get; set; } = [];
    }

    public sealed class RecebimentosFluxoNoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    public sealed class RecebimentosFluxoLigacaoDto
    {
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public decimal Kg { get; set; }
    }

    public sealed class RecebimentosPlanejamentoDto
    {
        public Guid ItemId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Fornecedor { get; set; } = string.Empty;
        public decimal PlanejadoKg { get; set; }
        public decimal RecebidoKg { get; set; }
        public decimal? CumprimentoPct { get; set; }
        public string Situacao { get; set; } = string.Empty;
    }

    public sealed class RecebimentosDivergenciaDto
    {
        public Guid AgendamentoId { get; set; }
        public DateTime DataInicio { get; set; }
        public string? Placa { get; set; }
        public long? NotaNumero { get; set; }
        public string? Fornecedor { get; set; }
        public string? Produto { get; set; }
        public decimal PesoNotaKg { get; set; }
        public decimal PesoRecebidoKg { get; set; }
        public decimal DiferencaKg { get; set; }
        public decimal? DiferencaPct { get; set; }
    }

    public sealed class RecebimentosMatrizDto
    {
        public List<string> Produtos { get; set; } = [];
        public List<RecebimentosMatrizLinhaDto> Linhas { get; set; } = [];
        public Dictionary<string, decimal> TotalPorProduto { get; set; } = [];
        public decimal TotalGeral { get; set; }
    }

    public sealed class RecebimentosMatrizLinhaDto
    {
        public string Fornecedor { get; set; } = string.Empty;
        public Dictionary<string, decimal> Valores { get; set; } = [];
        public decimal Total { get; set; }
        public int Descargas { get; set; }
    }
}
