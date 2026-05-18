using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Motorista
{
    public class UserMotoristaUpdateDto
    {
        public string? Username { get; set; }
        public string? NomeReal { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Telefone { get; set; }
        public string? PlacaVeiculo { get; set; }
        public string? TipoVeiculo { get; set; }
    }
}
