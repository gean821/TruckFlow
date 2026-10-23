using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

        public async Task HandleAsync(
            AgendamentoCanceladoEvent domainEvent,
            CancellationToken cancellationToken
            )
        {
            if (domainEvent.MotoristaUsuarioId is null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var corpo = string.IsNullOrWhiteSpace(domainEvent.MotivoCancelamento)
                ? "Seu agendamento foi cancelado."
                : $"Seu agendamento foi cancelado. Motivo: {domainEvent.MotivoCancelamento}";

            var agendamentoInfo = await _db.Agendamento
                .AsNoTracking()
                .Where(a => a.Id == domainEvent.AgendamentoId)
                .Select(a => new
                {
                    a.DataInicio,
                    UnidadeEntregaNome = a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    LocalDescargaNome = a.LocalDescarga != null ? a.LocalDescarga.Nome : null,
                    EmpresaNome = a.Empresa != null ? a.Empresa.NomeFantasia : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                motivo = domainEvent.MotivoCancelamento,
                unidadeEntregaNome = agendamentoInfo?.UnidadeEntregaNome,
                localDescargaNome = agendamentoInfo?.LocalDescargaNome,
                dataAgendamentoIso = agendamentoInfo?.DataInicio,
                empresaNome = agendamentoInfo?.EmpresaNome,
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
        }
    }
}
