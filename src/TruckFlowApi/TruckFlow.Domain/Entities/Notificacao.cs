using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class Notificacao : EntidadeBase, IEmpresaScoped
    {
        public Guid EmpresaId { get; set; }

        public Guid DestinatarioUsuarioId { get; set; }
        public Usuario? Destinatario { get; set; }

        public TipoNotificacao Tipo { get; set; }

        public PrioridadeNotificacao Prioridade { get; set; }

        public required string Titulo { get; set; }

        public required string Corpo { get; set; }

        public required string Payload { get; set; }

        public DateTime? LidaEm { get; set; }

        public Guid? CorrelationId { get; set; }

        public ICollection<NotificacaoEntrega> Entregas { get; set; } = [];

        public void MarcarComoLida()
        {
            if (LidaEm.HasValue)
            {
                return;
            }

            LidaEm = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}