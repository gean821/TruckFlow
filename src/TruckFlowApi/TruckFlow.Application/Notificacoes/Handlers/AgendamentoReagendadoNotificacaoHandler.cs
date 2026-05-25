using System.Globalization;
using System.Text.Json;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes.Handlers
{
    public sealed class AgendamentoReagendadoNotificacaoHandler
        : IDomainEventHandler<AgendamentoEvent.AgendamentoReagendadoEvent>
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        private readonly AppDbContext _db;

        public AgendamentoReagendadoNotificacaoHandler(AppDbContext db)
        {
            _db = db;
        }

        public Task HandleAsync(
            AgendamentoEvent.AgendamentoReagendadoEvent domainEvent,
            CancellationToken cancellationToken)
        {
            if (domainEvent.MotoristaUsuarioId is null)
            {
                return Task.CompletedTask;
            }

            var now = DateTime.UtcNow;

            var dataNovaTexto = domainEvent.DataInicioNova
                .ToLocalTime()
                .ToString("dd/MM 'às' HH:mm", PtBr);

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                dataInicioAnterior = domainEvent.DataInicioAnterior,
                dataFimAnterior = domainEvent.DataFimAnterior,
                dataInicioNova = domainEvent.DataInicioNova,
                dataFimNova = domainEvent.DataFimNova,
            }, OutboxEventSerializer.Options);

            var notificacao = new Notificacao
            {
                EmpresaId = domainEvent.EmpresaId,
                DestinatarioUsuarioId = domainEvent.MotoristaUsuarioId.Value,
                Tipo = TipoNotificacao.AgendamentoReagendado,
                Prioridade = PrioridadeNotificacao.Alta,
                Titulo = "Agendamento reagendado",
                Corpo = $"Seu agendamento foi reagendado para {dataNovaTexto}.",
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