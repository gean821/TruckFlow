using System.Collections.Frozen;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public static class RelatorioOrdenacao
    {
        public const string Janela = "janela";
        public const string Status = "status";
        public const string Motorista = "motorista";
        public const string Placa = "placa";
        public const string Fornecedor = "fornecedor";
        public const string Produto = "produto";
        public const string Unidade = "unidade";
        public const string Doca = "doca";
        public const string Peso = "peso";
        public const string Chegada = "chegada";
        public const string FimDescarga = "fimDescarga";

        public static readonly FrozenSet<string> Todas = new[]
        {
            Janela, Status, Motorista, Placa, Fornecedor, Produto, Unidade, Doca, Peso, Chegada, FimDescarga
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RelatorioAgendamentoDetalhadoQueryDto : RelatorioAgendamentoFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public sealed record RelatorioResumoLinha(
        Guid Id,
        StatusAgendamento Status,
        DateTime DataInicio,
        DateTime DataFim,
        Guid? LocalDescargaId,
        DateTime? ChegouEm,
        DateTime? InicioDescargaEm,
        DateTime? FimDescargaEm,
        DateTime? SaiuEm,
        decimal PesoKg);

    public sealed record FimDescargaDoca(Guid LocalDescargaId, DateTime FimDescargaEm);

    public sealed class AgendamentoDetalhadoDto
    {
        public Guid Id { get; init; }
        public DateTime DataInicio { get; init; }
        public DateTime DataFim { get; init; }
        public required string Status { get; init; }
        public required string StatusRotulo { get; init; }
        public required string Origem { get; init; }
        public string? MotoristaNome { get; init; }
        public string? MotoristaTelefone { get; init; }
        public string? PlacaVeiculo { get; init; }
        public string? TipoVeiculo { get; init; }
        public string? FornecedorNome { get; init; }
        public required string Produto { get; init; }
        public string? UnidadeEntrega { get; init; }
        public string? LocalDescarga { get; init; }
        public decimal? PesoKg { get; init; }
        public long? NotaFiscalNumero { get; init; }
        public DateTime? ChegouEm { get; init; }
        public DateTime? InicioDescarga { get; init; }
        public bool InicioEstimado { get; init; }
        public DateTime? FimDescargaEm { get; init; }
        public DateTime? Saida { get; init; }
        public bool SaidaEstimada { get; init; }
        public double? EsperaMin { get; init; }
        public double? DescargaMin { get; init; }
        public double? PermanenciaMin { get; init; }
        public double? AtrasoChegadaMin { get; init; }
        public string? SituacaoChegada { get; init; }
    }

    public sealed class RelatorioDetalhadoStatusDto
    {
        public required string Status { get; init; }
        public required string Rotulo { get; init; }
        public int Quantidade { get; init; }
    }

    public sealed class RelatorioDetalhadoResumoDto
    {
        public int Total { get; init; }
        public decimal PesoTotalKg { get; init; }
        public IReadOnlyList<RelatorioDetalhadoStatusDto> PorStatus { get; init; } = [];
        public int Chegaram { get; init; }
        public int Adiantados { get; init; }
        public int NoHorario { get; init; }
        public int Atrasados { get; init; }
        public int NaoCompareceram { get; init; }
        public decimal? PontualidadePct { get; init; }
        public double? AtrasoMedioMin { get; init; }
        public double? EsperaMediaMin { get; init; }
        public double? EsperaP90Min { get; init; }
        public double? DescargaMediaMin { get; init; }
        public double? PermanenciaMediaMin { get; init; }
        public bool TemposEstimados { get; init; }
    }
}
