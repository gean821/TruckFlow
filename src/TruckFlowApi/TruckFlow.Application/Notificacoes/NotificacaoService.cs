using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class NotificacaoService : INotificacaoService
    {
        private const int MaxTake = 100;

        private readonly INotificacaoRepositorio _repo;
        private readonly ICurrentUserService _user;

        public NotificacaoService(
            INotificacaoRepositorio repo,
            ICurrentUserService user)
        {
            _repo = repo;
            _user = user;
        }

        public async Task<IReadOnlyList<NotificacaoListItemDto>> ListarMinhasAsync(
            int skip,
            int take,
            CancellationToken ct)
        {
            var userId = _user.UserId;
            var effectiveTake = Math.Clamp(take, 1, MaxTake);
            var effectiveSkip = Math.Max(skip, 0);

            var notificacoes = await _repo.ListByUserAsync(
                userId,
                effectiveSkip,
                effectiveTake,
                ct);

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

        public async Task<int> ContarNaoLidasMinhasAsync(CancellationToken ct)
        {
            return await _repo.CountUnreadByUserAsync(_user.UserId, ct);
        }

        public async Task<bool> MarcarComoLidaAsync(
            Guid notificacaoId,
            CancellationToken ct)
        {
            var userId = _user.UserId;
            var notificacao = await _repo.GetByIdForUserAsync(notificacaoId, userId, ct);

            if (notificacao is null)
            {
                return false;
            }

            if (notificacao.LidaEm.HasValue)
            {
                return true;
            }

            notificacao.MarcarComoLida();
            await _repo.UpdateAsync(notificacao, ct);
            return true;
        }
    }
}