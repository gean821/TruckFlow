using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Agendamento
{
    //esse response será o responsável por mostrar os dados para o MOTORISTA no app mobile. Há dois tipos:
    //1- Response de agendamento do Motorista no app
    //2- Response de agendamento para o Admin no sistema.
    public sealed class AgendamentoMotoristaResponse
    {
        public required Guid Id { get; set; }
        public string? LocalDescarga { get; set; }
        public required string UnidadeEntrega { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public required string Fornecedor { get; set; }
        public required string Produto { get; set; }
        public decimal? PesoCarga { get; set; }
        public required string NotaFiscal { get; set; }
        public required string PlacaVeiculo { get; set; }

        public DateTime HorarioInicio { get; set; }
        public string? TipoVeiculo { get; set; }
        public required DateTime CreatedAt { get; set; }
        public string? Status { get; set; } // opcional

        public bool Congelado { get; set; }
        public string? MotivoCongelamento { get; set; }
    }
}
