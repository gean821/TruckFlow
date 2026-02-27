using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Empresa
{
    public sealed class RegisterEmpresaAdminDto
    {
        public required string RazaoSocial { get; init; }
        public required string NomeFantasia { get; init; }
        public required string Cnpj { get; init; }
        public required string EmailEmpresa { get; init; }
        public required string NomeAdmin { get; init; }
        public required string Username { get; init; }
        public required string EmailAdmin { get; init; }
        public required string Telefone { get; init; }
        public required string Password { get; init; }

        public required string Logradouro { get; init; }
        public required string Cidade { get; init; }
        public required string Bairro { get; init; }
        public required string Estado { get; init; }
        public required string Cep { get; init; }
        public required string Numero { get; set; }


    }
}
