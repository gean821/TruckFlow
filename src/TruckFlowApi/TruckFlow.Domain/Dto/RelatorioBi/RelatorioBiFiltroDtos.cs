using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class RelatorioBiFiltroDto
    {
        public DateOnly? Inicio { get; set; }
        public DateOnly? Fim { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
    }

    public sealed record RelatorioBiCriteria(
        DateTime DesdeUtc,
        DateTime AteUtc,
        Guid? UnidadeEntregaId,
        Guid? LocalDescargaId,
        Guid? FornecedorId,
        Guid? ProdutoId);

    public sealed record RelatorioBiLinha(
        Guid Id,
        StatusAgendamento Status,
        DateTime DataInicio,
        DateTime DataFim,
        decimal PesoKg,
        Guid? FornecedorId,
        string? FornecedorNome,
        Guid? ProdutoId,
        string? ProdutoNome,
        Guid? UnidadeEntregaId,
        string? UnidadeEntregaNome,
        Guid? LocalDescargaId,
        string? LocalDescargaNome,
        DateTime? ChegouEm,
        DateTime? InicioDescargaEm,
        DateTime? FimDescargaEm,
        DateTime? SaiuEm,
        string? PlacaVeiculo = null,
        string? MotoristaNome = null);

    public sealed record RelatorioBiDoca(Guid Id, string Nome, string? UnidadeEntregaNome);

    public sealed class RelatorioBiPeriodoDto
    {
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public DateOnly InicioAnterior { get; set; }
        public DateOnly FimAnterior { get; set; }
        public int Dias { get; set; }
    }

    public sealed class RelatorioBiIndicadorDto
    {
        public decimal Valor { get; set; }
        public decimal Anterior { get; set; }
        public decimal? VariacaoPct { get; set; }
        public List<decimal> Serie { get; set; } = [];
    }

    public sealed class RelatorioBiRankingDto
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Agendamentos { get; set; }
        public int Concluidos { get; set; }
        public decimal PesoKg { get; set; }
        public decimal Percentual { get; set; }
    }
}
