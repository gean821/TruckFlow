using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Empresa
{
    public sealed class EmpresaResponseDto
    {
        public Guid Id { get; init; }
        public string RazaoSocial { get; init; } = default!;
        public string NomeFantasia { get; init; } = default!;
        public string Cnpj { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string Cidade { get; init; } = default!;
        public string Estado { get; init; } = default!;
        public bool Ativa { get; init; }
        public DateTime CreatedAt { get; init; }
    }

}
