using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public sealed class AgendamentoRelatorioLinha
    {
        public string? FornecedorNome { get; init; }
        public string? ProdutoNome { get; init; }
        public TipoCarga TipoCarga { get; init; }
        public string? PlacaVeiculo { get; init; }
        public string? MotoristaNome { get; init; }
        public string? UnidadeEntrega { get; init; }
        public string? LocalDescarga { get; init; }
        public TipoVeiculo? TipoVeiculo { get; init; }
        public decimal? PesoCarga { get; init; }
        public StatusAgendamento Status { get; init; }
        public DateTime DataInicio { get; init; }
        public DateTime DataFim { get; init; }
        public string Produto => ProdutoNome ?? TipoCarga.ToString(); // Se o agendamento n tiver nome de prod. cai pro tipo de carga
    }
}
