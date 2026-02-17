using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Empresa
{
    public sealed class EmpresaCreateDto
    {
        public required string RazaoSocial { get; init; }
        public required string NomeFantasia { get; init; }
        public required string Cnpj { get; init; }

        public required string Email { get; init; }
        public required string Telefone { get; init; }
        public required string Cep { get; init; }
        public required string Logradouro { get; init; }
        public required string Numero { get; init; }
        public string? Complemento { get; init; }
        public required string Bairro { get; init; }
        public required string Cidade { get; init; }
        public required string Estado { get; init; }
    }
}
