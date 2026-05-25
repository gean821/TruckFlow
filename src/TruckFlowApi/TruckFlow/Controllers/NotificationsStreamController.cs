using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Domain.Contracts;
using TruckFlow.Extensions.Notificacao;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/notifications")]
    public sealed class NotificationsStreamController : ControllerBase
    {
        private readonly SseNotificationStreamer _streamer;
        private readonly ICurrentUserService _user;

        public NotificationsStreamController(
            SseNotificationStreamer streamer,
            ICurrentUserService user)
        {
            _streamer = streamer;
            _user = user;
        }


        [Authorize]
        [HttpGet("stream")]
        public Task Stream(CancellationToken cancellationToken)
        {
            var userId = _user.UserIdOrNull
                ?? throw new UnauthorizedAccessException("Usuário não identificado.");

            return _streamer.StreamAsync(Response, userId, cancellationToken);
        }
    }
}