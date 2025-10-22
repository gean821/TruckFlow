using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Enums;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Domain.Dto.NotaFiscal
{
    public class NotaFiscalParsedDto
    {
        public required string ChaveAcesso { get; set; }
        public required long Numero { get; set; }
        public required string Fornecedor { get; set; }
        public required string Serie { get; set; }
        public required DateTime DataEmissao { get; set; }
        public required string EmitenteNome { get; set; }
        public required string EmitenteCnpj { get; set; }
        public required string DestinatarioNome { get; set; }
        public required string DestinatarioCpfCnpj { get; set; }
        public required decimal ValorTotal { get; set; }
        public required decimal? PesoBruto { get; set; }
        public int? VolumeQuantidade { get; set; }
        public required string PlacaVeiculo { get; set; }
        public required IEnumerable<NotaFiscalItemDto> Itens { get; set; }
        public required TipoCarga TipoCarga { get; set; } = TipoCarga.Indefinido;
        public IEnumerable<string>? ValidationWarnings { get; set; }
    }
}
