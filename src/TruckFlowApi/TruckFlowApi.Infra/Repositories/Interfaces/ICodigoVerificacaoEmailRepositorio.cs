using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Interfaces
{
    public interface ICodigoVerificacaoEmailRepositorio
    {
        Task<CodigoVerificacaoEmail?> ObterUltimoAtivoAsync(Guid usuarioId, FinalidadeVerificacaoEmail finalidade, CancellationToken token = default);
        Task<int> ContarEnviosRecentesAsync(Guid usuarioId, FinalidadeVerificacaoEmail finalidade, TimeSpan janela, CancellationToken token = default);
        Task InvalidarAnterioresAsync(Guid usuarioId, FinalidadeVerificacaoEmail finalidade, CancellationToken token = default);
        Task AdicionarAsync(CodigoVerificacaoEmail codigo, CancellationToken token = default);
        Task SalvarAsync(CancellationToken token = default);
    }
}
