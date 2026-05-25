using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Events
{
    public sealed record AgendamentoCanceladoEvent(
        Guid AgendamentoId,
        Guid EmpresaId,
        Guid? MotoristaUsuarioId,
        string? MotivoCancelamento,
        DateTime OcorridoEm
    ) : IDomainEvent
    {
        public string IdempotencyKey => $"agendamento-cancelado:{AgendamentoId}";
    }
}