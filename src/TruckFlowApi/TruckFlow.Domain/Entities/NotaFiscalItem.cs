using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Entities
{
    public class NotaFiscalItem : EntidadeBase
    {
        public Guid NotaFiscalId { get; set; }
        public required NotaFiscal NotaFiscal { get; set; }
        public required string Codigo { get; set; }
        public required string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public string? Unidade { get; set; }
        public required decimal ValorUnitario { get; set; }
        public required decimal ValorTotal { get; set; }
    }
}
