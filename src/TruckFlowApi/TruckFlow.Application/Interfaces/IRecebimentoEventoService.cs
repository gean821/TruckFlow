using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Interfaces
{
    public interface IRecebimentoEventoService
    {
       Task RegistrarRecebimentoManual(
       Guid itemPlanejamentoId,
       decimal quantidade,
       string? observacao,
       CancellationToken token
        );
    }
}
