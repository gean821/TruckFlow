using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Extensions.Auth;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IWebHostEnvironment _env;

        public AuthController(
            IAuthService authService,
            IRefreshTokenService refreshTokenService,
            IWebHostEnvironment env)
        {
            _authService = authService;
            _refreshTokenService = refreshTokenService;
            _env = env;
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken ct = default)
        {
            var raw = Request.Cookies[RefreshCookieHelper.CookieName];

            if (string.IsNullOrEmpty(raw))
                return Unauthorized();

            var result = await _authService.RefreshAccessTokenAsync(
                raw,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct);

            if (!result.Success)
            {
                RefreshCookieHelper.ClearRefresh(Response, _env);
                return Unauthorized();
            }

            RefreshCookieHelper.SetRefresh(
                Response,
                _env,
                result.NewRefreshToken!,
                result.NewRefreshExpiresAt!.Value);

            return Ok(new RefreshResponseDto
            {
                Token = result.AccessToken!,
                TokenExpiresAt = result.AccessExpiresAt!.Value
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh-mobile")]
        public async Task<IActionResult> RefreshMobile(
            [FromBody] RefreshMobileRequestDto dto,
            CancellationToken ct = default)
        {
            var result = await _authService.RefreshAccessTokenAsync(
                dto.RefreshToken,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct);

            if (!result.Success)
                return Unauthorized();

            return Ok(new RefreshMobileResponseDto
            {
                Token = result.AccessToken!,
                TokenExpiresAt = result.AccessExpiresAt!.Value,
                RefreshToken = result.NewRefreshToken!,
                RefreshTokenExpiresAt = result.NewRefreshExpiresAt!.Value
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            var raw = Request.Cookies[RefreshCookieHelper.CookieName];

            if (!string.IsNullOrEmpty(raw))
                await _refreshTokenService.RevokeAsync(raw, "logout", ct);

            RefreshCookieHelper.ClearRefresh(Response, _env);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("logout-mobile")]
        public async Task<IActionResult> LogoutMobile(
            [FromBody] LogoutMobileRequestDto dto,
            CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(dto.RefreshToken))
                await _refreshTokenService.RevokeAsync(dto.RefreshToken, "logout", ct);

            return NoContent();
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll(CancellationToken ct = default)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _refreshTokenService.RevokeAllForUserAsync(userId, "logout-all", ct);
            RefreshCookieHelper.ClearRefresh(Response, _env);

            return NoContent();
        }
    }
}