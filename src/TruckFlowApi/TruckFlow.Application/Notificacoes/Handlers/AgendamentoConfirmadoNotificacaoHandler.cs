using System.Text.Json;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes.Handlers
{
    public sealed class AgendamentoConfirmadoNotificacaoHandler
        : IDomainEventHandler<AgendamentoEvent.AgendamentoConfirmadoEvent>
    {
        private readonly AppDbContext _db;

        public AgendamentoConfirmadoNotificacaoHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task HandleAsync(
            AgendamentoEvent.AgendamentoConfirmadoEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var dados = await NotificacaoMotorista.CarregarAgendamentoAsync(_db, domainEvent.AgendamentoId, cancellationToken);
            var dataInicio = dados?.DataInicio ?? domainEvent.DataInicio;
            var local = NotificacaoMotorista.DescreverLocal(dados);

            var corpo = local is null
                ? $"Descarga marcada para {NotificacaoMotorista.FormatarDataHora(dataInicio)}."
                : $"Descarga marcada para {NotificacaoMotorista.FormatarDataHora(dataInicio)} em {local}.";

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
                TipoNotificacao.AgendamentoConfirmado,
                PrioridadeNotificacao.Normal,
                "Agendamento confirmado",
                corpo,
                payload,
                DateTime.UtcNow));
        }
    }
}
