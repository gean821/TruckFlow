using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class NotificacaoEntrega : EntidadeBase, IEmpresaScoped
    {
        public Guid EmpresaId { get; set; }

        public Guid NotificacaoId { get; set; }
        public Notificacao? Notificacao { get; set; }

        public CanalNotificacao Canal { get; set; }

        public StatusEntregaNotificacao Status { get; set; }

        public int TentativasEfetuadas { get; set; }

        public DateTime? ProximaTentativaEm { get; set; }

        public DateTime? UltimaTentativaEm { get; set; }

        public string? ProviderMessageId { get; set; }

        public string? Erro { get; set; }

        public DateTime? ReceiptCheckedAt { get; set; }

        public void MarcarEnviado(string? providerMessageId = null)
        {
            Status = StatusEntregaNotificacao.Enviado;
            ProviderMessageId = providerMessageId;
            UltimaTentativaEm = DateTime.UtcNow;
            Erro = null;
            ProximaTentativaEm = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RegistrarFalha
            (
                string erro,
                TimeSpan? proximoBackoff
            )
        {
            TentativasEfetuadas++;
            UltimaTentativaEm = DateTime.UtcNow;
            Erro = erro;

            if (proximoBackoff.HasValue)
            {
                Status = StatusEntregaNotificacao.Pendente;
                ProximaTentativaEm = DateTime.UtcNow.Add(proximoBackoff.Value);
            }
            else
            {
                Status = StatusEntregaNotificacao.Falhou;
                ProximaTentativaEm = null;
            }

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
