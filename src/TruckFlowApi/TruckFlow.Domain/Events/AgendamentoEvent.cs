using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Events
{
    public static partial class AgendamentoEvent
    {
        public sealed record MotoristaChegouEvent(
            Guid AgendamentoId,
            Guid EmpresaId,
            Guid MotoristaUsuarioId,
            DateTime OcorridoEm
        ) : IDomainEvent
        {
            public string IdempotencyKey => $"motorista-chegou:{AgendamentoId}";
        }

        public sealed record AgendamentoReagendadoEvent(
            Guid AgendamentoId,
            Guid EmpresaId,
            Guid? MotoristaUsuarioId,
            DateTime DataInicioAnterior,
            DateTime DataFimAnterior,
            DateTime DataInicioNova,
            DateTime DataFimNova,
            DateTime OcorridoEm
        ) : IDomainEvent
        {
            public string IdempotencyKey =>
                $"agendamento-reagendado:{AgendamentoId}:{DataInicioNova:O}";
        }
    }
}