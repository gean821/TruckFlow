using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

        public async Task HandleAsync(
            AgendamentoEvent.AgendamentoReagendadoEvent domainEvent,
            CancellationToken cancellationToken)
        {
            if (domainEvent.MotoristaUsuarioId is null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var dataNovaTexto = domainEvent.DataInicioNova
                .ToLocalTime()
                .ToString("dd/MM 'às' HH:mm", PtBr);

            var agendamentoInfo = await _db.Agendamento
                .AsNoTracking()
                .Where(a => a.Id == domainEvent.AgendamentoId)
                .Select(a => new
                {
                    UnidadeEntregaNome = a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    LocalDescargaNome = a.LocalDescarga != null ? a.LocalDescarga.Nome : null,
                    EmpresaNome = a.Empresa != null ? a.Empresa.NomeFantasia : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                dataInicioAnterior = domainEvent.DataInicioAnterior,
                dataFimAnterior = domainEvent.DataFimAnterior,
                dataInicioNova = domainEvent.DataInicioNova,
                dataFimNova = domainEvent.DataFimNova,
                unidadeEntregaNome = agendamentoInfo?.UnidadeEntregaNome,
                localDescargaNome = agendamentoInfo?.LocalDescargaNome,
                dataAgendamentoIso = domainEvent.DataInicioNova,
                empresaNome = agendamentoInfo?.EmpresaNome,
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
        }
    }
}