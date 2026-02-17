using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Empresa
{
    public sealed class EmpresaUpdateDto
    {
        public string? RazaoSocial { get; init; }
        public string? NomeFantasia { get; init; }
        public string? Email { get; init; }
        public string? Telefone { get; init; }

        public string? Cep { get; init; }
        public string? Logradouro { get; init; }
        public string? Numero { get; init; }
        public string? Complemento { get; init; }
        public string? Bairro { get; init; }
        public string? Cidade { get; init; }
        public string? Estado { get; init; }
    }

}
