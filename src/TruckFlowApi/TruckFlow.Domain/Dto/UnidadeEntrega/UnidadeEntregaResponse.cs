using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.UnidadeEntrega
{
    public sealed class UnidadeEntregaResponse
    {
        public Guid Id { get; init; }

        public string Nome { get; init; } = default!;
        public string Localizacao { get; init; } = default!;
        public string? Logradouro { get; init; }
        public string? Numero { get; init; }
        public string? Complemento { get; init; }
        public string? Bairro { get; init; }
        public string? Cidade { get; init; }
        public string? Estado { get; init; }
        public string? Cep { get; init; }

        public string? Empresa { get; set; }

        public double? Latitude { get; init; }
        public double? Longitude { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }

        public bool? Ativa { get; set; } = false;
    }

}
