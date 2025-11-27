using TruckFlow.Application.Enums;

namespace TruckFlow.Domain.Entities
{
    public class NotaFiscal : EntidadeBase
    {
        public required string ChaveAcesso { get; set; } // 44 chars - único
        public required long Numero { get; set; }
        public required string Serie { get; set; }
        public required DateTime DataEmissao { get; set; }
        public required string EmitenteNome { get; set; }
        public required string EmitenteCnpj { get; set; }
        public required string DestinatarioNome { get; set; }
        public required string DestinatarioCpfCnpj { get; set; }   // pode ser CPF ou CNPJ
        public required decimal ValorTotal { get; set; }
        public required decimal? PesoBruto { get; set; }
        public decimal? PesoLiquido { get; set; }
        public int? VolumeQuantidade { get; set; }       // volumes/boxes etc.
        public string? PlacaVeiculo { get; set; }        // pode vir do XML ou ser informado pelo motorista
        public string? RawXml { get; set; }               // salvar XML (text)
        public NotaFiscalStatus Status { get; set; }
        public Guid FornecedorId { get; set; }
        public required Fornecedor Fornecedor { get; set; }
        public Guid? AgendamentoId { get; set; }
        public Agendamento? Agendamento { get; set; }
        public ICollection<NotaFiscalItem> Itens { get; set; } = new List<NotaFiscalItem>();
        public Guid UploadedByUserId { get; set; }       // quem enviou (motorista)
        public DateTime? UploadedAt { get; set; }
        public string? ValidationMessages { get; set; }  // lista/JSON de mensagens de validação
        public required TipoCarga TipoCarga { get; set; }
    }
}