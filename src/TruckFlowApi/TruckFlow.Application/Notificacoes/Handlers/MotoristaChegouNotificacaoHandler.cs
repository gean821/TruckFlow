using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Application.Notificacoes.Handlers
{
    public sealed class MotoristaChegouNotificacaoHandler
        : IDomainEventHandler<AgendamentoEvent.MotoristaChegouEvent>
    {
        private readonly AppDbContext _db;
        private readonly UserManager<Usuario> _userManager;

        public MotoristaChegouNotificacaoHandler(
            AppDbContext db,
            UserManager<Usuario> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task HandleAsync(
            AgendamentoEvent.MotoristaChegouEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var adminsDaEmpresa = admins
                .Where(u => u.EmpresaId == domainEvent.EmpresaId)
                .ToList();

            if (adminsDaEmpresa.Count == 0)
            {
                return;
            }

            var motorista = await _db.Users
                .Where(u => u.Id == domainEvent.MotoristaUsuarioId)
                .Select(u => new { u.Motorista!.NomeReal })
                .FirstOrDefaultAsync(cancellationToken);

            var agendamentoInfo = await _db.Agendamento
                .AsNoTracking()
                .Where(a => a.Id == domainEvent.AgendamentoId)
                .Select(a => new
                {
                    a.DataInicio,
                    UnidadeEntregaNome = a.UnidadeEntrega != null ? a.UnidadeEntrega.Nome : null,
                    LocalDescargaNome = a.LocalDescarga != null ? a.LocalDescarga.Nome : null,
                    EmpresaNome = a.Empresa != null ? a.Empresa.NomeFantasia : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            var nomeMotorista = motorista?.NomeReal ?? "Motorista";
            var now = DateTime.UtcNow;

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = domainEvent.AgendamentoId,
                motoristaUsuarioId = domainEvent.MotoristaUsuarioId,
                unidadeEntregaNome = agendamentoInfo?.UnidadeEntregaNome,
                localDescargaNome = agendamentoInfo?.LocalDescargaNome,
                dataAgendamentoIso = agendamentoInfo?.DataInicio,
                empresaNome = agendamentoInfo?.EmpresaNome,
            }, OutboxEventSerializer.Options);

            foreach (var admin in adminsDaEmpresa)
            {
                var notificacao = new Notificacao
                {
                    EmpresaId = domainEvent.EmpresaId,
                    DestinatarioUsuarioId = admin.Id,
                    Tipo = TipoNotificacao.MotoristaChegou,
                    Prioridade = PrioridadeNotificacao.Normal,
                    Titulo = "Motorista chegou",
                    Corpo = $"{nomeMotorista} chegou para o agendamento.",
                    Payload = payload,
                    CreatedAt = now,
                };

                notificacao.Entregas.Add(new NotificacaoEntrega
                {
                    EmpresaId = domainEvent.EmpresaId,
                    Canal = CanalNotificacao.InApp,
                    Status = StatusEntregaNotificacao.Enviado,
                    UltimaTentativaEm = now,
                    CreatedAt = now,
                });

                _db.Notificacao.Add(notificacao);
            }
        }
    }
}