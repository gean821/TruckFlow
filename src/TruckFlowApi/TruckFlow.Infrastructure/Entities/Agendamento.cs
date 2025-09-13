using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Infrastructure.Entities
{
    public class Agendamento : EntidadeBase
    {
        public required Usuario Usuario;
        public required Guid UsuarioId { get; set; }
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public required TipoCarga TipoCarga { get; set; }
        public required string VolumeCarga { get; set; }
        public StatusAgendamento StatusAgendamento { get; set; }
        public required UnidadeEntrega UnidadeEntrega { get; set; }
        public required Guid UnidadeEntregaId { get; set; }
        public required NotaFiscal NotaFiscal { get; set; }
        public required Guid NotaFiscalId { get; set; }
        public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}