using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public class OutboxEvent : EntidadeBase
    {
        public required string EventType { get; set; }

        public required string Payload { get; set; }

        public required string IdempotencyKey { get; set; }

        public Guid EmpresaId { get; set; }

        public DateTime OcorridoEm { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int Tentativas { get; set; }

        public DateTime? ProximaTentativaEm { get; set; }

        public string? UltimoErro { get; set; }

        public Guid? CorrelationId { get; set; }

        public void MarcarProcessado()
        {
            ProcessedAt = DateTime.UtcNow;
            UltimoErro = null;
            ProximaTentativaEm = null;
        }

        public void RegistrarFalha
            (
                string erro,
                TimeSpan proximoBackoff
            )
        {
            Tentativas++;
            UltimoErro = erro;
            ProximaTentativaEm = DateTime.UtcNow.Add(proximoBackoff);
        }
    }
}
