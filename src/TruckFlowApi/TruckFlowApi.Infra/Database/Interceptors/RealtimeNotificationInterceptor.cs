using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlowApi.Infra.Database.Interceptors
{
    public sealed class RealtimeNotificationInterceptor : SaveChangesInterceptor
    {
        public const string ChannelName = "notif_realtime";

        private readonly ILogger<RealtimeNotificationInterceptor> _logger;
        private readonly List<NotificacaoEventDto> _pendingPayloads = new();

        public RealtimeNotificationInterceptor(ILogger<RealtimeNotificationInterceptor> logger)
        {
            _logger = logger;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CollectPendingPayloads(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CollectPendingPayloads(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await PublishAndClearAsync(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges
            (
                SaveChangesCompletedEventData eventData,
                int result
            )
        {
            PublishAndClearAsync(
                eventData.Context,
                CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return base.SavedChanges(eventData, result);
        }

        private void CollectPendingPayloads(DbContext? ctx)
        {
            _pendingPayloads.Clear();

            if (ctx is null)
            {
                return;
            }

            var notificacoes = ctx.ChangeTracker
                .Entries<Notificacao>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var n in notificacoes)
            {
                _pendingPayloads.Add(new NotificacaoEventDto(
                    EmpresaId: n.EmpresaId,
                    UsuarioId: n.DestinatarioUsuarioId,
                    NotificacaoId: n.Id,
                    Tipo: (int)n.Tipo,
                    Prioridade: (int)n.Prioridade,
                    Titulo: n.Titulo,
                    Corpo: n.Corpo,
                    CriadaEm: n.CreatedAt));
            }
        }

        private async ValueTask PublishAndClearAsync
            (
                DbContext? ctx,
                CancellationToken cancellationToken
            )
        {
            if (_pendingPayloads.Count == 0 || ctx is null)
            {
                _pendingPayloads.Clear();
                return;
            }

            foreach (var payload in _pendingPayloads)
            {
                try
                {
                    var json = JsonSerializer.Serialize(payload, OutboxEventSerializer.Options);
                    await ctx.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_notify({ChannelName}, {json})",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Falha em pg_notify pra Notificacao {Id}; real-time perdido (front pode recarregar).",
                        payload.NotificacaoId);
                }
            }

            _pendingPayloads.Clear();
        }
    }
}