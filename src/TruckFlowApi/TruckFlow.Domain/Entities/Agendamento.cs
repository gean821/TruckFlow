using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;


namespace TruckFlow.Domain.Entities
{
    public class Agendamento : EntidadeBase
    {
        public Grade? Grade { get; set; }
        public Guid? GradeId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? UsuarioId { get; set; }
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public required TipoCarga TipoCarga { get; set; }
        public decimal? VolumeCarga { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public StatusAgendamento StatusAgendamento { get; set; }
        public required UnidadeEntrega UnidadeEntrega { get; set; }
        public required Guid UnidadeEntregaId { get; set; }
        public NotaFiscal? NotaFiscal { get; set; }
        public Guid? NotaFiscalId { get; set; }
        public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();

        public void AlterarStatus(StatusAgendamento novoStatus)
        {
            if (!StatusAgendamento.PodeTransitarPara(novoStatus))
            {
                throw new Exception(
                    $"Transição inválida: {StatusAgendamento} → {novoStatus}");
            }

            StatusAgendamento = novoStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reservar(Guid usuarioId, NotaFiscal notaFiscal)
        {
            if (!StatusAgendamento.PodeTransitarPara(StatusAgendamento.Confirmado))
            {
                throw new Exception("Transição de status inválida.");
            }

            if (FornecedorId != notaFiscal.FornecedorId)
            {
                throw new Exception("A Nota Fiscal não pertence ao fornecedor desta vaga.");
            }

            StatusAgendamento = StatusAgendamento.Confirmado;
            UsuarioId = usuarioId;
            NotaFiscalId = notaFiscal.Id;
            VolumeCarga = notaFiscal.PesoBruto ?? 0;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
