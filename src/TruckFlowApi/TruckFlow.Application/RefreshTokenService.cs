using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const int DefaultLifetimeDays = 30;
        private const int RawTokenBytes = 64;
        private const int DeviceInfoMaxLength = 512;
        private const int IpAddressMaxLength = 64;

        private readonly IRefreshTokenRepositorio _repo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            IRefreshTokenRepositorio repo,
            IConfiguration configuration,
            ILogger<RefreshTokenService> logger)
        {
            _repo = repo;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RefreshTokenIssueResult> IssueAsync(
            Guid userId,
            Guid? empresaId,
            string? deviceInfo,
            string? ipAddress,
            Guid? familyId = null,
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var raw = GenerateRawToken();
            var hash = HashToken(raw);

            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmpresaId = empresaId,
                TokenHash = hash,
                FamilyId = familyId ?? Guid.NewGuid(),
                DeviceInfo = Truncate(deviceInfo, DeviceInfoMaxLength),
                IpAddress = Truncate(ipAddress, IpAddressMaxLength),
                CreatedAt = now,
                ExpiresAt = now.AddDays(GetLifetimeDays())
            };

            await _repo.AddAsync(token, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "RefreshToken emitido. UserId={UserId} FamilyId={FamilyId} TokenId={TokenId}",
                userId, token.FamilyId, token.Id);

            return new RefreshTokenIssueResult(raw, token.ExpiresAt, token.FamilyId, token.Id);
        }

        public async Task<RefreshTokenRotateResult> RotateAsync(
            string rawToken,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return RefreshTokenRotateResult.Fail(RefreshTokenRotateFailure.NotFound);

            var now = DateTime.UtcNow;
            var hash = HashToken(rawToken);

            var current = await _repo.GetByHashAsync(hash, ct);

            if (current is null)
                return RefreshTokenRotateResult.Fail(RefreshTokenRotateFailure.NotFound);

            if (current.RevokedAt is not null)
            {
                _logger.LogWarning(
                    "Reuse de refresh token detectado. TokenId={TokenId} UserId={UserId} FamilyId={FamilyId} OriginalRevokedAt={RevokedAt} Reason={Reason}",
                    current.Id, current.UserId, current.FamilyId, current.RevokedAt, current.RevokedReason);

                await _repo.RevokeFamilyAsync(current.FamilyId, now, "reuse-detected", ct);
                return RefreshTokenRotateResult.Fail(RefreshTokenRotateFailure.ReuseDetected);
            }

            if (current.ExpiresAt <= now)
                return RefreshTokenRotateResult.Fail(RefreshTokenRotateFailure.Expired);

            var newRaw = GenerateRawToken();
            var newHash = HashToken(newRaw);
            var newToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = current.UserId,
                EmpresaId = current.EmpresaId,
                TokenHash = newHash,
                FamilyId = current.FamilyId,
                DeviceInfo = Truncate(deviceInfo, DeviceInfoMaxLength) ?? current.DeviceInfo,
                IpAddress = Truncate(ipAddress, IpAddressMaxLength) ?? current.IpAddress,
                CreatedAt = now,
                ExpiresAt = now.AddDays(GetLifetimeDays())
            };

            current.RevokedAt = now;
            current.RevokedReason = "rotated";
            current.ReplacedByTokenId = newToken.Id;

            await _repo.AddAsync(newToken, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "RefreshToken rotacionado. UserId={UserId} FamilyId={FamilyId} OldTokenId={OldTokenId} NewTokenId={NewTokenId}",
                current.UserId, current.FamilyId, current.Id, newToken.Id);

            return RefreshTokenRotateResult.Ok(newRaw, newToken.ExpiresAt, newToken.Id, current.UserId, current.FamilyId);
        }

        public async Task<bool> RevokeAsync(string rawToken, string reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return false;

            var hash = HashToken(rawToken);
            var token = await _repo.GetByHashAsync(hash, ct);

            if (token is null || token.RevokedAt is not null) return false;

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "RefreshToken revogado. TokenId={TokenId} UserId={UserId} Reason={Reason}",
                token.Id, token.UserId, reason);

            return true;
        }

        public async Task<int> RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default)
        {
            var affected = await _repo.RevokeFamilyAsync(
                familyId,
                 DateTime.UtcNow,
                  reason,
                  ct);

            if (affected > 0)
            {
                _logger.LogInformation(
                    "Família de refresh tokens revogada. FamilyId={FamilyId} Affected={Affected} Reason={Reason}",
                    familyId, affected, reason);
            }

            return affected;
        }

        public async Task<int> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
        {
            var affected = await _repo.RevokeAllForUserAsync(
                userId,
                 DateTime.UtcNow,
                  reason,
                  ct);

            if (affected > 0)
            {
                _logger.LogInformation(
                    "Todos refresh tokens do usuário revogados. UserId={UserId} Affected={Affected} Reason={Reason}",
                    userId, affected, reason);
            }

            return affected;
        }

        private int GetLifetimeDays()
        {
            var value = _configuration["RefreshTokenOptions:LifetimeDays"];
            return int.TryParse(value, out var days) && days > 0 ? days : DefaultLifetimeDays;
        }

        private static string GenerateRawToken()
        {
            var bytes = new byte[RawTokenBytes];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string HashToken(string raw)
        {
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        private static string? Truncate(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) ? value :
            value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}