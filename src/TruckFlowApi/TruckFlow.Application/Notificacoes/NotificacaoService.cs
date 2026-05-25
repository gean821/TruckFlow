using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Dto.Shared;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class NotificacaoService : INotificacaoService
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

        private readonly INotificacaoRepositorio _repo;
        private readonly ICurrentUserService _user;
        private readonly IEmpresaContext _empresaContext;

        public NotificacaoService(
            INotificacaoRepositorio repo,
            ICurrentUserService user,
            IEmpresaContext empresaContext)
        {
            _repo = repo;
            _user = user;
            _empresaContext = empresaContext;
        }

        public async Task<PagedResponse<NotificacaoListItemDto>> ListarMinhasAsync(
            NotificacaoListQueryDto query,
            CancellationToken ct)
        {
            var sanitized = new NotificacaoListQueryDto
            {
                PageNumber = Math.Max(query.PageNumber, 1),
                PageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize),
                UnreadOnly = query.UnreadOnly,
                Tipo = query.Tipo,
                Prioridade = query.Prioridade,
            };

            var (items, totalCount) = _user.IsMotorista
                ? await _repo.ListByUserPagedAcrossTenantsAsync(_user.UserId, sanitized, ct)
                : await _repo.ListByUserPagedAsync(_user.UserId, sanitized, ct);

            var totalPages = sanitized.PageSize == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)sanitized.PageSize);

            return new PagedResponse<NotificacaoListItemDto>
            {
                Items = items
                    .Select(n => new NotificacaoListItemDto(
                        n.Id,
                        (int)n.Tipo,
                        (int)n.Prioridade,
                        n.Titulo,
                        n.Corpo,
                        n.CreatedAt,
                        n.LidaEm,
                        n.Payload))
                    .ToList(),
                PageNumber = sanitized.PageNumber,
                PageSize = sanitized.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
            };
        }

        public Task<int> ContarNaoLidasMinhasAsync(CancellationToken ct) =>
            _user.IsMotorista
                ? _repo.CountUnreadByUserAcrossTenantsAsync(_user.UserId, ct)
                : _repo.CountUnreadByUserAsync(_user.UserId, ct);

        public async Task<bool> MarcarComoLidaAsync(Guid notificacaoId, CancellationToken ct)
        {
            var userId = _user.UserId;
            var isMotorista = _user.IsMotorista;

            var notificacao = isMotorista
                ? await _repo.GetByIdForUserAcrossTenantsAsync(notificacaoId, userId, ct)
                : await _repo.GetByIdForUserAsync(notificacaoId, userId, ct);

            if (notificacao is null)
            {
                return false;
            }

            if (notificacao.LidaEm.HasValue)
            {
                return true;
            }

            notificacao.MarcarComoLida();

            if (isMotorista)
            {
                using var tenantScope = _empresaContext.WithTenant(notificacao.EmpresaId);
                await _repo.UpdateAsync(notificacao, ct);
            }
            else
            {
                await _repo.UpdateAsync(notificacao, ct);
            }

            return true;
        }

        public async Task<IReadOnlyList<NotificacaoListItemDto>> ListarPorAgendamentoAsync(
            Guid agendamentoId,
            CancellationToken ct)
        {
            var userId = _user.UserId;
            var notificacoes = _user.IsMotorista
                ? await _repo.ListByUserAndAgendamentoAcrossTenantsAsync(userId, agendamentoId, ct)
                : await _repo.ListByUserAndAgendamentoAsync(userId, agendamentoId, ct);

            return notificacoes
                .Select(n => new NotificacaoListItemDto(
                    n.Id,
                    (int)n.Tipo,
                    (int)n.Prioridade,
                    n.Titulo,
                    n.Corpo,
                    n.CreatedAt,
                    n.LidaEm,
                    n.Payload))
                .ToList();
        }
    }
}