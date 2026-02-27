using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.UnidadeEntrega
{
    public sealed class UnidadeEntregaUpdateDto
    {
        public string? Nome { get; init; }
        public string? Localizacao { get; init; }

        public string? Logradouro { get; init; }
        public string? Numero { get; init; }
        public string? Complemento { get; init; }
        public string? Bairro { get; init; }
        public string? Cidade { get; init; }
        public string? Estado { get; init; }
        public string? Cep { get; init; }

        public double? Latitude { get; init; }
        public double? Longitude { get; init; }

        public bool? Ativa { get; set; }
     }

}
