using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Outbox;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class NotificacaoSendService : INotificacaoSendService
    {
        private const int TituloMaxLength = 120;
        private const int CorpoMaxLength = 2000;

        private readonly INotificacaoRepositorio _notificacaoRepo;
        private readonly IAgendamentoRepositorio _agendamentoRepo;
        private readonly UserManager<Usuario> _userManager;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmpresaContext _empresaContext;
        private readonly ILogger<NotificacaoSendService> _logger;

        public NotificacaoSendService(
            INotificacaoRepositorio notificacaoRepo,
            IAgendamentoRepositorio agendamentoRepo,
            UserManager<Usuario> userManager,
            ICurrentUserService currentUser,
            IEmpresaContext empresaContext,
            ILogger<NotificacaoSendService> logger)
        {
            _notificacaoRepo = notificacaoRepo;
            _agendamentoRepo = agendamentoRepo;
            _userManager = userManager;
            _currentUser = currentUser;
            _empresaContext = empresaContext;
            _logger = logger;
        }

        public async Task EnviarParaMotoristaAsync(
            EnviarParaMotoristaDto dto,
            CancellationToken ct)
        {
            var nomeAutor = _currentUser.NomeAutor ?? "-";
            ValidarTexto(dto.Titulo, dto.Corpo);

            var agendamento = await _agendamentoRepo.GetById(dto.AgendamentoId, ct)
                ?? throw new NotFoundException("Agendamento não encontrado.");

            GarantirAgendamentoAtivo(agendamento);

            if (agendamento.UsuarioId is null)
            {
                throw new BusinessException("Agendamento não tem motorista reservado — não há destinatário.");
            }

            var motoristaUsuarioId = agendamento.UsuarioId.Value;
            var autorUsuarioId = _currentUser.UserId;
            var now = DateTime.UtcNow;

            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = agendamento.Id,
                autorUsuarioId,
                autorNome =  nomeAutor,
                autorTipo = "Admin",
            }, OutboxEventSerializer.Options);

            var notificacao = new Notificacao
            {
                EmpresaId = agendamento.EmpresaId,
                DestinatarioUsuarioId = motoristaUsuarioId,
                Tipo = TipoNotificacao.MensagemManualAdmin,
                Prioridade = PrioridadeNotificacao.Normal,
                Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? nomeAutor : dto.Titulo.Trim(),
                Corpo = dto.Corpo,
                Payload = payload,
                CreatedAt = now,
            };

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = agendamento.EmpresaId,
                Canal = CanalNotificacao.InApp,
                Status = StatusEntregaNotificacao.Enviado,
                UltimaTentativaEm = now,
                CreatedAt = now,
            });

            notificacao.Entregas.Add(new NotificacaoEntrega
            {
                EmpresaId = agendamento.EmpresaId,
                Canal = CanalNotificacao.Push,
                Status = StatusEntregaNotificacao.Pendente,
                CreatedAt = now,
            });

            await _notificacaoRepo.AddAsync(notificacao, ct);

            _logger.LogInformation(
                "Mensagem manual enviada pelo admin {AutorUsuarioId} → motorista {MotoristaUsuarioId} (agendamento {AgendamentoId}, empresa {EmpresaId}).",
                autorUsuarioId, motoristaUsuarioId, agendamento.Id, agendamento.EmpresaId);
        }

        public async Task EnviarParaEmpresaAsync(
            EnviarParaEmpresaDto dto,
            CancellationToken ct)
        {
            ValidarTexto(dto.Titulo, dto.Corpo);
            var nomeAutor = _currentUser.NomeAutor ?? "-";

            var agendamento = await _agendamentoRepo.GetByIdAcrossTenants(dto.AgendamentoId, ct)
                ?? throw new NotFoundException("Agendamento não encontrado.");

            GarantirAgendamentoAtivo(agendamento);

            var motoristaUserId = _currentUser.UserId;

            if (agendamento.UsuarioId != motoristaUserId)
            {
                throw new UnauthorizedAccessException("Você só pode enviar mensagens sobre seus próprios agendamentos.");
            }

            using var tenant = _empresaContext.WithTenant(agendamento.EmpresaId);

            var adminsDaEmpresa = await BuscarAdminsDaEmpresaAsync(agendamento.EmpresaId);

            if (adminsDaEmpresa.Count == 0)
            {
                throw new BusinessException("Empresa sem administradores cadastrados.");
            }

            var now = DateTime.UtcNow;
            var payload = JsonSerializer.Serialize(new
            {
                agendamentoId = agendamento.Id,
                autorUsuarioId = motoristaUserId,
                autorNome = nomeAutor,
                autorTipo = "Motorista",
            }, OutboxEventSerializer.Options);

            var notificacoes = adminsDaEmpresa.Select(admin =>
            {
                var notificacao = new Notificacao
                {
                    EmpresaId = agendamento.EmpresaId,
                    DestinatarioUsuarioId = admin.Id,
                    Tipo = TipoNotificacao.MensagemManualMotorista,
                    Prioridade = PrioridadeNotificacao.Normal,
                    Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? nomeAutor : dto.Titulo.Trim(),
                    Corpo = dto.Corpo,
                    Payload = payload,
                    CreatedAt = now,
                };

                notificacao.Entregas.Add(new NotificacaoEntrega
                {
                    EmpresaId = agendamento.EmpresaId,
                    Canal = CanalNotificacao.InApp,
                    Status = StatusEntregaNotificacao.Enviado,
                    UltimaTentativaEm = now,
                    CreatedAt = now,
                });

                return notificacao;
            }).ToList();

            await _notificacaoRepo.AddRangeAsync(notificacoes, ct);

            _logger.LogInformation(
                "Mensagem manual enviada pelo motorista {MotoristaUsuarioId} → {AdminsCount} admin(s) da empresa {EmpresaId} (agendamento {AgendamentoId}).",
                motoristaUserId, notificacoes.Count, agendamento.EmpresaId, agendamento.Id);
        }

        private async Task<List<Usuario>> BuscarAdminsDaEmpresaAsync(Guid empresaId)
        {
            var todosAdmins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            return todosAdmins.Where(u => u.EmpresaId == empresaId).ToList();
        }

        private static void GarantirAgendamentoAtivo(Agendamento agendamento)
        {
            if (agendamento.DeletedAt.HasValue)
            {
                throw new BusinessException("Agendamento foi removido e não aceita novas mensagens.");
            }

            var statusAceitos = new[]
            {
                StatusAgendamento.Agendado,
                StatusAgendamento.EmAndamento,
            };

            if (!statusAceitos.Contains(agendamento.StatusAgendamento))
            {
                throw new BusinessException(
                    $"Agendamento em status '{agendamento.StatusAgendamento}' não aceita mensagens. " +
                    "Mensagens são permitidas enquanto a vaga está Agendada ou Em Andamento.");
            }
        }

        private static void ValidarTexto(
            string? titulo,
            string corpo)
        {
            if (!string.IsNullOrWhiteSpace(titulo) && titulo.Length > TituloMaxLength)
            {
                throw new BusinessException($"Título excede {TituloMaxLength} caracteres.");
            }

            if (string.IsNullOrWhiteSpace(corpo))
            {
                throw new BusinessException("Corpo obrigatório.");
            }

            if (corpo.Length > CorpoMaxLength)
            {
                throw new BusinessException($"Corpo excede {CorpoMaxLength} caracteres.");
            }
        }
    }
}