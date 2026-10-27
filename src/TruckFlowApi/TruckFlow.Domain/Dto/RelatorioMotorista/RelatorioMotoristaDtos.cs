using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.RelatorioMotorista
{
    public sealed class RelatorioMotoristaQueryDto
    {
        public DateOnly? Inicio { get; set; }
        public DateOnly? Fim { get; set; }
    }

    public sealed record RelatorioMotoristaLinha(
        Guid Id,
        StatusAgendamento Status,
        DateTime DataInicio,
        decimal PesoKg,
        string? Produto,
        string? UnidadeEntrega,
        string? LocalDescarga,
        string? Empresa);

    public sealed class RelatorioMotoristaResponseDto
    {
        public DateOnly Inicio { get; set; }
        public DateOnly Fim { get; set; }
        public DateOnly InicioAnterior { get; set; }
        public DateOnly FimAnterior { get; set; }
        public RelatorioMotoristaResumoDto Atual { get; set; } = new();
        public RelatorioMotoristaResumoDto Anterior { get; set; } = new();
        public decimal? VariacaoEntregasPct { get; set; }
        public decimal? VariacaoPesoPct { get; set; }
        public List<RelatorioMotoristaMesDto> HistoricoMensal { get; set; } = [];
        public List<RelatorioMotoristaFatiaDto> PorProduto { get; set; } = [];
        public List<RelatorioMotoristaFatiaDto> PorLocal { get; set; } = [];
        public RelatorioMotoristaProximoDto? ProximoAgendamento { get; set; }
    }

    public sealed class RelatorioMotoristaResumoDto
    {
        public int Agendamentos { get; set; }
        public int Concluidas { get; set; }
        public int NoPatio { get; set; }
        public int Aguardando { get; set; }
        public int Canceladas { get; set; }
        public int NaoCompareceu { get; set; }
        public decimal PesoKg { get; set; }
        public decimal? ComparecimentoPct { get; set; }
    }

    public sealed class RelatorioMotoristaMesDto
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public string Rotulo { get; set; } = string.Empty;
        public int Entregas { get; set; }
        public decimal PesoKg { get; set; }
    }

    public sealed class RelatorioMotoristaFatiaDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Detalhe { get; set; }
        public int Entregas { get; set; }
        public decimal PesoKg { get; set; }
        public decimal Percentual { get; set; }
    }

    public sealed class RelatorioMotoristaProximoDto
    {
        public Guid Id { get; set; }
        public DateTime DataInicio { get; set; }
        public string? UnidadeEntrega { get; set; }
        public string? LocalDescarga { get; set; }
        public string? Empresa { get; set; }
    }
}
