using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Application.Interfaces
{
    public interface INotaFiscalService
    {
        Task<NotaFiscalParsedDto> ParseXmlAsync(Stream xmlStream, CancellationToken token);
        Task<NotaFiscalParsedDto> SaveParsedNotaAsync(NotaFiscalParsedDto dto, Guid uploadedByUserId, CancellationToken token);

        Task<NotaFiscalParsedDto?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token);
    }
}
