using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public sealed class RelatorioArquivoDto
    {
        public required string ContentType { get; init; }
        public required string NomeArquivo { get; init; }
        public required Func<Stream, CancellationToken, Task> EscreverConteudo { get; init; }
    }
}