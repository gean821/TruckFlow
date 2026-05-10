using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.Agendamento
{
    public sealed class AgendamentoExpirationWorker : BackgroundService
    {
        private static readonly TimeSpan IntervaloExecucao = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan IntervaloPosErro = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AgendamentoExpirationWorker> _logger;

        public AgendamentoExpirationWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AgendamentoExpirationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "AgendamentoExpirationWorker iniciado. Intervalo: {Intervalo}.",
                IntervaloExecucao);

            while (!stoppingToken.IsCancellationRequested)
            {
                var proximoDelay = IntervaloExecucao;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAgendamentoExpirationService>();
                    await service.ExpirarVencidosAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no ciclo de expiração de agendamentos.");
                    proximoDelay = IntervaloPosErro;
                }

                try
                {
                    await Task.Delay(proximoDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("AgendamentoExpirationWorker finalizado.");
        }
    }
}
