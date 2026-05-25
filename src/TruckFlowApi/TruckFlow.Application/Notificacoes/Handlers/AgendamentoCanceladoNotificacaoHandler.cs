using System.Text.Json;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes.Handlers
{
    public sealed class AgendamentoCanceladoNotificacaoHandler
        : IDomainEventHandler<AgendamentoCanceladoEvent>
    {
        private readonly AppDbContext _db;

        public AgendamentoCanceladoNotificacaoHandler(AppDbContext db)
        {
            _db = db;
        }

        public Task HandleAsync(
            AgendamentoCanceladoEvent domainEvent,
            CancellationToken cancellationToken
            )
        {
            if (domainEvent.MotoristaUsuarioId is null)
            {
                return Task.CompletedTask;
            }

            var now = DateTime.UtcNow;

            var corpo = string.IsNullOrWhiteSpace(domainEvent.MotivoCancelamento)
                ? "Seu agendamento foi cancelado."
                : $"Seu agendamento foi cancelado. Motivo: {domainEvent.MotivoCancelamento}";

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                motivo = domainEvent.MotivoCancelamento,
            }, OutboxEventSerializer.Options);

            var notificacao = new Notificacao
            {
                EmpresaId = domainEvent.EmpresaId,
                DestinatarioUsuarioId = domainEvent.MotoristaUsuarioId.Value,
                Tipo = TipoNotificacao.AgendamentoCancelado,
                Prioridade = PrioridadeNotificacao.Alta,
                Titulo = "Agendamento cancelado",
                Corpo = corpo,
                Payload = payload,
                CreatedAt = now,
            };

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = domainEvent.EmpresaId,
                Canal = CanalNotificacao.InApp,
                Status = StatusEntregaNotificacao.Enviado,
                UltimaTentativaEm = now,
                CreatedAt = now,
            });

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = domainEvent.EmpresaId,
                Canal = CanalNotificacao.Push,
                Status = StatusEntregaNotificacao.Pendente,
                CreatedAt = now,
            });

            _db.Notificacao.Add(notificacao);

            return Task.CompletedTask;
        }
    }
}
