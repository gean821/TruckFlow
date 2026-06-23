using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Interfaces
{
    public interface IVerificacaoEmailService
    {
        Task EnviarCodigoAsync(Guid usuarioId, FinalidadeVerificacaoEmail finalidade, CancellationToken token = default);
        Task<string> ValidarCodigoAsync(Guid usuarioId, string codigo, FinalidadeVerificacaoEmail finalidade, CancellationToken token = default);
        (Guid UsuarioId, FinalidadeVerificacaoEmail Finalidade) ExtrairCodigoToken(string codigoToken);
    }
}
