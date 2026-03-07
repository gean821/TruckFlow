using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class NotaFiscalItem : EntidadeBase, IEmpresaScoped
    {
        public Guid NotaFiscalId { get; set; }
        public required NotaFiscal NotaFiscal { get; set; }
        public required string Codigo { get; set; }
        public required string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public string? Unidade { get; set; }
        public required decimal ValorUnitario { get; set; }
        public required decimal ValorTotal { get; set; }

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
    }
}
