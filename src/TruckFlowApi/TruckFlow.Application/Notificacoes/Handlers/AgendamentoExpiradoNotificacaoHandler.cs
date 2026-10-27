using System.Text.Json;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes.Handlers
{
    public sealed class AgendamentoExpiradoNotificacaoHandler
        : IDomainEventHandler<AgendamentoEvent.AgendamentoExpiradoEvent>
    {
        private readonly AppDbContext _db;

        public AgendamentoExpiradoNotificacaoHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task HandleAsync(
            AgendamentoEvent.AgendamentoExpiradoEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var dados = await NotificacaoMotorista.CarregarAgendamentoAsync(_db, domainEvent.AgendamentoId, cancellationToken);
            var dataInicio = dados?.DataInicio ?? domainEvent.DataInicio;

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                unidadeEntregaNome = dados?.UnidadeEntregaNome,
                localDescargaNome = dados?.LocalDescargaNome,
                dataAgendamentoIso = dataInicio,
                empresaNome = dados?.EmpresaNome,
            }, OutboxEventSerializer.Options);

            _db.Notificacao.Add(NotificacaoMotorista.Criar(
                domainEvent.EmpresaId,
                domainEvent.MotoristaUsuarioId,
                TipoNotificacao.AgendamentoExpirado,
                PrioridadeNotificacao.Alta,
                "Agendamento expirado",
                $"O horário de {NotificacaoMotorista.FormatarDataHora(dataInicio)} passou sem check-in. Se ainda precisa descarregar, faça um novo agendamento.",
                payload,
                DateTime.UtcNow));
        }
    }
}
