using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Notifications.Expo;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class ReceiptPollerWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan IdadeMinimaReceipt = TimeSpan.FromMinutes(2);
        private const int BatchSize = 100;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReceiptPollerWorker> _logger;

        public ReceiptPollerWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ReceiptPollerWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "ReceiptPollerWorker iniciado. Intervalo={Intervalo}, IdadeMinima={IdadeMinima}.",
                IdleDelay, IdadeMinimaReceipt);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = IdleDelay;

                try
                {
                    await PollOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no ciclo do ReceiptPoller.");
                    delay = ErrorDelay;
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("ReceiptPollerWorker finalizado.");
        }

        private async Task PollOnceAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var entregaRepo = sp.GetRequiredService<INotificacaoEntregaRepositorio>();
            var dispositivoRepo = sp.GetRequiredService<IDispositivoUsuarioRepositorio>();
            var expo = sp.GetRequiredService<IExpoPushClient>();

            var entregas = await entregaRepo.ClaimLotePushPendenteReceiptAsync(BatchSize, IdadeMinimaReceipt, token);
            if (entregas.Count == 0) return;

            // 1 entrega pode ter múltiplos tickets (1 por device) separados por vírgula.
            var ticketToEntrega = new Dictionary<string, NotificacaoEntrega>();
            foreach (var entrega in entregas)
            {
                if (string.IsNullOrEmpty(entrega.ProviderMessageId)) continue;
                foreach (var ticketId in entrega.ProviderMessageId.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    ticketToEntrega[ticketId.Trim()] = entrega;
                }
            }

            if (ticketToEntrega.Count == 0)
            {
                // Sem ticket válido (caso esquisito) — marca checked pra não reprocessar.
                await entregaRepo.MarcarReceiptVerificadoEmLoteAsync(
                    entregas.Select(e => e.Id).ToList(),
                    token);
                return;
            }

            Dictionary<string, ExpoPushReceipt> receipts;
            try
            {
                receipts = await expo.GetReceiptsAsync(ticketToEntrega.Keys.ToList(), token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Expo /getReceipts falhou; tentaremos no próximo ciclo.");
                return;
            }

            foreach (var (ticketId, entrega) in ticketToEntrega)
            {
                if (!receipts.TryGetValue(ticketId, out var receipt))
                {
                    continue;
                }

                if (receipt.Status == "ok") continue;

                var errCode = receipt.Details?.Error;
                _logger.LogWarning(
                    "Receipt Expo NOK ticket {Ticket} entrega {Entrega}: status={Status}, error={Error}, msg={Msg}.",
                    ticketId, entrega.Id, receipt.Status, errCode, receipt.Message);

                if (errCode == "DeviceNotRegistered" && !string.IsNullOrEmpty(receipt.Details?.ExpoPushToken))
                {
                    var dispositivo = await dispositivoRepo.GetByTokenAsync(receipt.Details.ExpoPushToken, token);
                    if (dispositivo is not null && dispositivo.Ativo)
                    {
                        dispositivo.Desativar();
                        await dispositivoRepo.UpdateAsync(dispositivo, token);
                        _logger.LogInformation(
                            "Dispositivo {DispositivoId} desativado por DeviceNotRegistered.",
                            dispositivo.Id);
                    }
                }
            }

            // Marca todas as entregas do lote como checadas (sucesso ou erro tratado).
            await entregaRepo.MarcarReceiptVerificadoEmLoteAsync(
                entregas.Select(e => e.Id).ToList(),
                token);
        }
    }
}