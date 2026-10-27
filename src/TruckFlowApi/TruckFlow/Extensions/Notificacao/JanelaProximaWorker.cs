using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class JanelaProximaWorker : BackgroundService
    {
        private static readonly TimeSpan IntervaloExecucao = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan IntervaloPosErro = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JanelaProximaWorker> _logger;

        public JanelaProximaWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<JanelaProximaWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "JanelaProximaWorker iniciado. Intervalo: {Intervalo}.",
                IntervaloExecucao);

            while (!stoppingToken.IsCancellationRequested)
            {
                var proximoDelay = IntervaloExecucao;

                try
                {
                    await EnviarPorEmpresaAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no ciclo de lembretes de janela próxima.");
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

            _logger.LogInformation("JanelaProximaWorker finalizado.");
        }

        private async Task EnviarPorEmpresaAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var empresaRepo = scope.ServiceProvider.GetRequiredService<IEmpresaRepositorio>();
            var empresaContext = scope.ServiceProvider.GetRequiredService<IEmpresaContext>();
            var service = scope.ServiceProvider.GetRequiredService<IJanelaProximaNotificacaoService>();

            var empresas = await empresaRepo.GetAll(stoppingToken);

            foreach (var empresa in empresas.Where(e => e.Ativa))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                using var tenantScope = empresaContext.WithTenant(empresa.Id);

                try
                {
                    await service.EnviarLembretesAsync(empresa.Id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falha ao enviar lembretes de janela da empresa {EmpresaId}. Próximas empresas seguem.",
                        empresa.Id);
                }
            }
        }
    }
}
