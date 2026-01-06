using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Motorista
{
    public class UserMotoristaResponseDto
    {
        public required Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Telefone { get; set; }
        public string? PlacaVeiculo { get; set; }
        public string? TipoVeiculo { get; set; }

        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt{ get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
