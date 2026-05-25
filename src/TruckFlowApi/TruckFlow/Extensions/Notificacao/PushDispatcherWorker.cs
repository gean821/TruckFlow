using System.Text.Json;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Notifications.Expo;
using TruckFlowApi.Infra.Outbox;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class PushDispatcherWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(15);
        private const int MaxTentativas = 4;

        private static readonly TimeSpan[] BackoffEscala =
        {
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(30),
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PushDispatcherWorker> _logger;

        public PushDispatcherWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<PushDispatcherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PushDispatcherWorker iniciado. IdleDelay={IdleDelay}.", IdleDelay);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = IdleDelay;

                try
                {
                    var processedAny = await ProcessNextAsync(stoppingToken);
                    if (processedAny)
                    {
                        delay = TimeSpan.Zero;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no ciclo do PushDispatcher.");
                    delay = ErrorDelay;
                }

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("PushDispatcherWorker finalizado.");
        }

        private async Task<bool> ProcessNextAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var entregaRepo = sp.GetRequiredService<INotificacaoEntregaRepositorio>();
            var dispositivoRepo = sp.GetRequiredService<IDispositivoUsuarioRepositorio>();
            var expo = sp.GetRequiredService<IExpoPushClient>();
            var empresaContext = sp.GetRequiredService<IEmpresaContext>();

            var db = sp.GetRequiredService<TruckFlowApi.Infra.Database.AppDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync(token);

            var entrega = await entregaRepo.ClaimNextPushPendenteAsync(token);
            if (entrega is null)
            {
                await tx.CommitAsync(token);
                return false;
            }

            // NotificacaoEntrega é IEmpresaScoped — o EmpresaScopedSaveChangesInterceptor exige
            // tenant ativo no SaveChanges. Worker não tem HTTP context, então fixamos pelo EmpresaId
            // da própria entrega claimed.
            using var tenant = empresaContext.WithTenant(entrega.EmpresaId);

            var notificacao = await entregaRepo.GetNotificacaoByEntregaAsync(entrega.Id, token);
            if (notificacao is null)
            {
                entrega.RegistrarFalha("Notificacao pai não encontrada.", null);
                await entregaRepo.SaveChangesAsync(token);
                await tx.CommitAsync(token);
                return true;
            }

            var dispositivos = await dispositivoRepo.GetAtivosByUsuarioAsync(notificacao.DestinatarioUsuarioId, token);
            if (dispositivos.Count == 0)
            {
                entrega.RegistrarFalha("Destinatário sem dispositivo Expo ativo.", null);
                await entregaRepo.SaveChangesAsync(token);
                await tx.CommitAsync(token);

                _logger.LogInformation(
                    "Entrega push {EntregaId} sem dispositivo — falha definitiva.",
                    entrega.Id);
                return true;
            }

            try
            {
                var messages = MontarMensagens(notificacao, dispositivos);
                var tickets = await expo.SendAsync(messages, token);

                var idsOk = tickets
                    .Where(t => t.Status == "ok" && !string.IsNullOrEmpty(t.Id))
                    .Select(t => t.Id!)
                    .ToList();

                if (idsOk.Count > 0)
                {
                    entrega.MarcarEnviado(string.Join(",", idsOk));
                }
                else
                {
                    var firstError = tickets.FirstOrDefault(t => t.Status != "ok");
                    var errMsg = firstError?.Message ?? "Todos os tickets retornaram erro.";
                    var backoff = ProximoBackoff(entrega.TentativasEfetuadas);
                    entrega.RegistrarFalha(errMsg, entrega.TentativasEfetuadas + 1 >= MaxTentativas ? null : backoff);
                }
            }
            catch (Exception ex)
            {
                var backoff = ProximoBackoff(entrega.TentativasEfetuadas);
                entrega.RegistrarFalha(
                    ex.Message,
                    entrega.TentativasEfetuadas + 1 >= MaxTentativas ? null : backoff);

                _logger.LogWarning(ex,
                    "Falha enviando push entrega {EntregaId} (tentativa {Tentativa}).",
                    entrega.Id, entrega.TentativasEfetuadas);
            }

            await entregaRepo.SaveChangesAsync(token);
            await tx.CommitAsync(token);

            _logger.LogInformation(
                "Push entrega {EntregaId} processada: Status={Status}, ProviderMessageId={ProviderMessageId}.",
                entrega.Id, entrega.Status, entrega.ProviderMessageId);

            return true;
        }

        private static List<ExpoPushMessage> MontarMensagens(
            TruckFlow.Domain.Entities.Notificacao notificacao,
            IReadOnlyList<DispositivoUsuario> dispositivos)
        {
            var payloadExtra = TryParsePayload(notificacao.Payload);

            return dispositivos.Select(d => new ExpoPushMessage
            {
                To = d.ExpoPushToken,
                Title = notificacao.Titulo,
                Body = notificacao.Corpo,
                Priority = notificacao.Prioridade == PrioridadeNotificacao.Critica ? "high" : "normal",
                Sound = "default",
                Data = new Dictionary<string, object?>
                {
                    ["notificacaoId"] = notificacao.Id,
                    ["tipo"] = (int)notificacao.Tipo,
                    ["agendamentoId"] = payloadExtra?.GetValueOrDefault("agendamentoId"),
                },
            }).ToList();
        }

        private static Dictionary<string, JsonElement>? TryParsePayload(string payload)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload, OutboxEventSerializer.Options);
            }
            catch
            {
                return null;
            }
        }

        private static TimeSpan ProximoBackoff(int tentativasJaFeitas)
        {
            var idx = Math.Min(tentativasJaFeitas, BackoffEscala.Length - 1);
            return BackoffEscala[idx];
        }
    }
}