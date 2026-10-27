using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class JanelaProximaNotificacaoService : IJanelaProximaNotificacaoService
    {
        private readonly AppDbContext _db;
        private readonly TimeProvider _timeProvider;
        private readonly JanelaProximaOptions _options;
        private readonly ILogger<JanelaProximaNotificacaoService> _logger;

        public JanelaProximaNotificacaoService(
            AppDbContext db,
            TimeProvider timeProvider,
            IOptions<JanelaProximaOptions> options,
            ILogger<JanelaProximaNotificacaoService> logger)
        {
            _db = db;
            _timeProvider = timeProvider;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<int> EnviarLembretesAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            var agora = _timeProvider.GetUtcNow().UtcDateTime;
            var limite = agora.AddMinutes(_options.AntecedenciaMinutos);

            var candidatos = await _db.Agendamento
                .Include(a => a.UnidadeEntrega)
                .Include(a => a.LocalDescarga)
                .Include(a => a.Empresa)
                .Where(a => a.EmpresaId == empresaId
                    && a.StatusAgendamento == StatusAgendamento.Agendado
                    && a.UsuarioId != null
                    && a.LembreteJanelaEnviadoEm == null
                    && a.DataInicio > agora
                    && a.DataInicio <= limite)
                .OrderBy(a => a.DataInicio)
                .Take(_options.LoteMaximo)
                .ToListAsync(cancellationToken);

            if (candidatos.Count == 0)
            {
                return 0;
            }

            foreach (var agendamento in candidatos)
            {
                agendamento.LembreteJanelaEnviadoEm = agora;

                var dados = new NotificacaoMotorista.DadosAgendamento(
                    agendamento.DataInicio,
                    agendamento.UnidadeEntrega?.Nome,
                    agendamento.LocalDescarga?.Nome,
                    agendamento.Empresa?.NomeFantasia);

                var local = NotificacaoMotorista.DescreverLocal(dados);
                var hora = NotificacaoMotorista.FormatarHora(agendamento.DataInicio);

                var payload = JsonSerializer.Serialize(new
                {
                    agendamentoId = agendamento.Id,
                    unidadeEntregaNome = dados.UnidadeEntregaNome,
                    localDescargaNome = dados.LocalDescargaNome,
                    dataAgendamentoIso = agendamento.DataInicio,
                    empresaNome = dados.EmpresaNome,
                }, OutboxEventSerializer.Options);

                _db.Notificacao.Add(NotificacaoMotorista.Criar(
                    agendamento.EmpresaId,
                    agendamento.UsuarioId!.Value,
                    TipoNotificacao.JanelaPropxima,
                    PrioridadeNotificacao.Alta,
                    Titulo(agendamento.DataInicio - agora),
                    local is null
                        ? $"Descarga às {hora}. Tenha a nota fiscal em mãos e siga para a portaria."
                        : $"Descarga às {hora} em {local}. Tenha a nota fiscal em mãos e siga para a portaria.",
                    payload,
                    agora));
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Conflito ao marcar lembretes de janela da empresa {EmpresaId}; o próximo ciclo reprocessa.",
                    empresaId);
                _db.ChangeTracker.Clear();
                return 0;
            }

            _logger.LogInformation(
                "Lembrete de janela enviado para {Total} agendamento(s) da empresa {EmpresaId}.",
                candidatos.Count, empresaId);

            return candidatos.Count;
        }

        public static string Titulo(TimeSpan faltando)
        {
            var minutos = (int)Math.Ceiling(faltando.TotalMinutes);

            if (minutos >= 55)
                return "Sua janela começa em 1 hora";

            return minutos <= 1
                ? "Sua janela começa agora"
                : $"Sua janela começa em {minutos} minutos";
        }
    }
}
