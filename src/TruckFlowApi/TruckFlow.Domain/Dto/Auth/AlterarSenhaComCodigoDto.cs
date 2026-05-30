using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Auth
{
    public sealed record AlterarSenhaComCodigoDto(
        string CodigoToken,
        string NovaSenha,
        string ConfirmarSenha
    );
}
