using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public sealed class AgendamentoAdminCreateDto
    {

        public required Guid FornecedorId { get; set; }        
        public Guid LocalDescargaId { get; set; }        
        public Guid? ProdutoId { get; set; }
        public Guid EmpresaId { get; set; }
        public required DateTime DataInicio { get; set; }
        public required DateTime DataFim { get; set; }
        public required TipoCarga TipoCarga { get;set;}
        public Guid? MotoristaId { get; set; }
        public Guid? NotaFiscalId { get; set; }
        public decimal VolumeCarga { get; set; }
        public string? PlacaVeiculo { get; set; } 
        public TipoVeiculo? TipoVeiculo { get; set; }
    }
}
