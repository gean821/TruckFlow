using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class OutboxProcessorWorker : BackgroundService
    {
        private const int BatchSize = 10;
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(10);

        private const int MaxTentativas = 8;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorWorker> _logger;

        public OutboxProcessorWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxProcessorWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "OutboxProcessorWorker iniciado. BatchSize={BatchSize}, IdleDelay={IdleDelay}.",
                BatchSize, IdleDelay);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delayBeforeNext = IdleDelay;

                try
                {
                    var processedAny = await ProcessBatchAsync(stoppingToken);
                    if (processedAny)
                    {
                        delayBeforeNext = TimeSpan.Zero;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha geral no ciclo do OutboxProcessor.");
                    delayBeforeNext = ErrorDelay;
                }

                if (delayBeforeNext > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delayBeforeNext, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("OutboxProcessorWorker finalizado.");
        }

        private async Task<bool> ProcessBatchAsync(CancellationToken token)
        {
            var processed = 0;
            for (var i = 0; i < BatchSize && !token.IsCancellationRequested; i++)
            {
                var didOne = await ProcessNextAsync(token);
                if (!didOne)
                {
                    break;
                }
                processed++;
            }

            return processed > 0;
        }

        private async Task<bool> ProcessNextAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AppDbContext>();
            var typeResolver = sp.GetRequiredService<IOutboxEventTypeResolver>();
            var empresaContext = sp.GetRequiredService<IEmpresaContext>();

            await using var tx = await db.Database.BeginTransactionAsync(token);

            var outboxEvent = await db.OutboxEvent
                .FromSqlRaw(@"
                    SELECT *, xmin FROM ""OutboxEvent""
                    WHERE ""ProcessedAt"" IS NULL
                      AND (""ProximaTentativaEm"" IS NULL OR ""ProximaTentativaEm"" <= NOW())
                    ORDER BY ""CreatedAt""
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1")
                .FirstOrDefaultAsync(token);

            if (outboxEvent is null)
            {
                await tx.CommitAsync(token);
                return false;
            }

            try
            {
                var eventType = typeResolver.Resolve(outboxEvent.EventType);
                var domainEvent = 
                    (IDomainEvent)OutboxEventSerializer.Deserialize(outboxEvent.Payload, eventType);

                using (empresaContext.WithTenant(outboxEvent.EmpresaId))
                {
                    var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                    var handler = sp.GetService(handlerType);

                    if (handler is null)
                    {
                        throw new InvalidOperationException(
                            $"Nenhum IDomainEventHandler registrado para '{outboxEvent.EventType}'.");
                    }

                    var handleMethod = handlerType
                        .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                            ?? throw new InvalidOperationException("HandleAsync não encontrado no handler.");

                    var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, token })!;
                    await task;

                    outboxEvent.MarcarProcessado();
                    await db.SaveChangesAsync(token);
                }

                await tx.CommitAsync(token);
                _logger.LogInformation(
                    "OutboxEvent {Id} processado ({EventType}).",
                    outboxEvent.Id, outboxEvent.EventType);
                
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(token);
                _logger.LogError(ex,
                    "Falha processando OutboxEvent {Id} ({EventType}), tentativa atual={Tentativas}.",
                    outboxEvent.Id, outboxEvent.EventType, outboxEvent.Tentativas);

                await RegistrarFalhaAsync(outboxEvent.Id, ex, token);
                return true;
            }
        }

        private async Task RegistrarFalhaAsync
            (
                Guid outboxEventId,
                Exception error,
                CancellationToken token
            )
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var stale = await db.OutboxEvent
                .FirstOrDefaultAsync(o => o.Id == outboxEventId, token);

            if (stale is null)
            {
                return;
            }

            var proximoBackoff = CalcularBackoff(stale.Tentativas);
            stale.RegistrarFalha(error.Message, proximoBackoff);

            if (stale.Tentativas >= MaxTentativas)
            {
                stale.ProximaTentativaEm = null;
                _logger.LogError(
                    "OutboxEvent {Id} excedeu MaxTentativas ({Max}); marcado como falha definitiva.",
                    outboxEventId, MaxTentativas);
            }

            await db.SaveChangesAsync(token);
        }

        private static TimeSpan CalcularBackoff(int tentativasAtuais)
        {
            // Exponencial: 30s, 1m, 2m, 4m, 8m, ..., cap em 10m.
            var seconds = Math.Min(30d * Math.Pow(2, tentativasAtuais), MaxBackoff.TotalSeconds);
            return TimeSpan.FromSeconds(seconds);
        }
    }
}