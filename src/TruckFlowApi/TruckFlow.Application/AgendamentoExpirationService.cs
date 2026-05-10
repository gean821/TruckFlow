using Microsoft.Extensions.Logging;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AgendamentoExpirationService : IAgendamentoExpirationService
    {
        private const int BatchSize = 500;

        private readonly IAgendamentoRepositorio _repo;
        private readonly ILogger<AgendamentoExpirationService> _logger;

        public AgendamentoExpirationService(
            IAgendamentoRepositorio repo,
            ILogger<AgendamentoExpirationService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<int> ExpirarVencidosAsync(CancellationToken cancellationToken = default)
        {
            var totalExpirados = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var referencia = DateTime.UtcNow;

                var candidatos = await _repo.GetExpiradosCandidatos(
                    referencia,
                    BatchSize,
                    cancellationToken);

                if (candidatos.Count == 0)
                {
                    break;
                }

                foreach (var agendamento in candidatos)
                {
                    if (!agendamento.PodeExpirarNaData(referencia))
                    {
                        continue;
                    }

                    agendamento.Expirar();
                    totalExpirados++;
                }

                await _repo.SaveChangesAsync(cancellationToken);

                if (candidatos.Count < BatchSize)
                {
                    break;
                }
            }

            if (totalExpirados > 0)
            {
                _logger.LogInformation(
                    "Expiração automática concluída: {Total} agendamento(s) movidos para Expirado.",
                    totalExpirados);
            }

            return totalExpirados;
        }
    }
}
