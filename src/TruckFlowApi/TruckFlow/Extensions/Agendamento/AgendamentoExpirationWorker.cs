using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlowApi.Infra.Repositories.Interfaces;

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
                    await ExpirarPorEmpresaAsync(stoppingToken);
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

        private async Task ExpirarPorEmpresaAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var empresaRepo = scope.ServiceProvider.GetRequiredService<IEmpresaRepositorio>();
            var empresaContext = scope.ServiceProvider.GetRequiredService<IEmpresaContext>();
            var service = scope.ServiceProvider.GetRequiredService<IAgendamentoExpirationService>();

            var empresas = await empresaRepo.GetAll(stoppingToken);

            foreach (var empresa in empresas)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                using var tenantScope = empresaContext.WithTenant(empresa.Id);

                try
                {
                    await service.ExpirarVencidosAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falha ao expirar agendamentos da empresa {EmpresaId}. Próximas empresas seguem.",
                        empresa.Id);
                }
            }
        }
    }
}
