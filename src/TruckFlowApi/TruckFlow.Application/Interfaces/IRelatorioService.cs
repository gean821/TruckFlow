using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Interfaces
{
    public interface IRelatorioService
    {
        Task<RelatorioArquivoDto> GerarRelatorioAgendamentos(
            RelatorioAgendamentoFilterDto filtros,
            FormatoRelatorio formato,
            CancellationToken token = default);
    }
}
