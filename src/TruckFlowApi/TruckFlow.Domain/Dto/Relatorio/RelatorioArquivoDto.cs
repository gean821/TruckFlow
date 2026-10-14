using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public class RelatorioArquivoDto
    {
        public required byte[] Conteudo { get; set; }
        public required string ContentType { get; set; }
        public required string NomeArquivo { get; set; }
    }
}
