using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public sealed record RelatorioAgendamentoCriteria
    {
        public required DateTime DataInicioUtc { get; init; }
        public required DateTime DataFimUtc { get; init; }
        public TimeSpan? HoraInicio { get; init; }
        public TimeSpan? HoraFim { get; init; }
        public Guid? FornecedorId { get; init; }
        public Guid? ProdutoId { get; init; }
        public string? PlacaVeiculo { get; init; }
        public string? Motorista { get; init; }
        public Guid? UnidadeEntregaId { get; init; }
        public Guid? LocalDescargaId { get; init; }
        public IReadOnlyList<StatusAgendamento> Status { get; init; } = [];
        public OrigemAgendamento? Origem { get; init; }
        public SituacaoChegada? Chegada { get; init; }
        public string? Busca { get; init; }
        public string OrdenarPor { get; init; } = RelatorioOrdenacao.Janela;
        public bool Desc { get; init; }
    }
}
