using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;

namespace TruckFlow.Application.Notificacoes
{
    public static class NotificacaoMotorista
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        public sealed record DadosAgendamento(
            DateTime DataInicio,
            string? UnidadeEntregaNome,
            string? LocalDescargaNome,
            string? EmpresaNome);

        public static string FormatarDataHora(DateTime dataUtc) =>
            TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc), Grade.OperationalTimeZone)
                .ToString("dd/MM 'às' HH:mm", PtBr);

        public static string FormatarHora(DateTime dataUtc) =>
            TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc), Grade.OperationalTimeZone)
                .ToString("HH:mm", PtBr);

        public static string? DescreverLocal(DadosAgendamento? dados)
        {
            if (dados is null)
                return null;

            var partes = new[] { dados.LocalDescargaNome, dados.UnidadeEntregaNome, dados.EmpresaNome }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            return partes.Length == 0 ? null : string.Join(" • ", partes);
        }

        public static Task<DadosAgendamento?> CarregarAgendamentoAsync(
            AppDbContext db,
            Guid agendamentoId,
            CancellationToken cancellationToken) =>
            db.Agendamento
                .AsNoTracking()
                .Where(a => a.Id == agendamentoId)
                .Select(a => new DadosAgendamento(
                    a.DataInicio,
                    a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    a.LocalDescarga != null ? a.LocalDescarga.Nome : null,
                    a.Empresa != null ? a.Empresa.NomeFantasia : null))
                .FirstOrDefaultAsync(cancellationToken);

        public static Notificacao Criar(
            Guid empresaId,
            Guid motoristaUsuarioId,
            TipoNotificacao tipo,
            PrioridadeNotificacao prioridade,
            string titulo,
            string corpo,
            string payload,
            DateTime agoraUtc)
        {
            var notificacao = new Notificacao
            {
                EmpresaId = empresaId,
                DestinatarioUsuarioId = motoristaUsuarioId,
                Tipo = tipo,
                Prioridade = prioridade,
                Titulo = titulo,
                Corpo = corpo,
                Payload = payload,
                CreatedAt = agoraUtc,
            };

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = empresaId,
                Canal = CanalNotificacao.InApp,
                Status = StatusEntregaNotificacao.Enviado,
                UltimaTentativaEm = agoraUtc,
                CreatedAt = agoraUtc,
            });

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = empresaId,
                Canal = CanalNotificacao.Push,
                Status = StatusEntregaNotificacao.Pendente,
                CreatedAt = agoraUtc,
            });

            return notificacao;
        }
    }
}
