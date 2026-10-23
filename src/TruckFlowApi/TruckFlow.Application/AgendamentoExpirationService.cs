using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AgendamentoExpirationService : IAgendamentoExpirationService
    {
        private const int BatchSize = 500;

        private readonly IAgendamentoRepositorio _repo;
        private readonly IAgendamentoRecebimentoLifecycleService _lifecycle;
        private readonly ILogger<AgendamentoExpirationService> _logger;

        public AgendamentoExpirationService(
            IAgendamentoRepositorio repo,
            IAgendamentoRecebimentoLifecycleService lifecycle,
            ILogger<AgendamentoExpirationService> logger)
        {
            _repo = repo;
            _lifecycle = lifecycle;
            _logger = logger;
        }

        public async Task<int> ExpirarVencidosAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var totalExpirados = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var referencia = DateTime.UtcNow;

                await using var tx = await _repo.BeginTransactionAsync(cancellationToken);

                var lockAdquirido = await _repo.TryAdvisoryLockEmpresaAsync(empresaId, cancellationToken);
                if (!lockAdquirido)
                {
                    _logger.LogInformation(
                        "Empresa {EmpresaId} já está sendo processada por outra instância neste ciclo — pulando.",
                        empresaId);
                    await tx.CommitAsync(cancellationToken);
                    break;
                }

                var candidatos = await _repo.GetExpiradosCandidatos(
                    referencia,
                    BatchSize,
                    cancellationToken);

                if (candidatos.Count == 0)
                {
                    await tx.CommitAsync(cancellationToken);
                    break;
                }

                foreach (var agendamento in candidatos)
                {
                    if (!agendamento.PodeExpirarNaData(referencia))
                    {
                        continue;
                    }

                    var precisaEstornar =
                        agendamento.StatusAgendamento == StatusAgendamento.Agendado;

                    agendamento.Expirar();
                    totalExpirados++;

                    if (precisaEstornar)
                    {
                        try
                        {
                            await _lifecycle.AoCancelarOuExpirarAsync(agendamento, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Falha ao estornar reserva do agendamento expirado {AgendamentoId}.",
                                agendamento.Id);
                        }
                    }
                }

                try
                {
                    await _repo.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogWarning(ex,
                        "Conflito de concorrência ao expirar agendamentos. " +
                        "Algum(ns) registro(s) foram modificados em paralelo " +
                        "(provavelmente reservados por motoristas). " +
                        "O próximo ciclo do worker reprocessará os pendentes.");
                    break;
                }

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
