using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Auth
{
        public sealed record VerificarCodigoEmailDto(string Codigo, FinalidadeVerificacaoEmail Finalidade);
}
