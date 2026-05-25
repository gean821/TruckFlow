using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<Usuario> _userManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUsuarioRepositorio _usuarioRepo;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IConfiguration configuration,
            UserManager<Usuario> userManager,
            IRefreshTokenService refreshTokenService,
            IUsuarioRepositorio usuarioRepo,
            ILogger<AuthService> logger
            )
        {
            _configuration = configuration;
            _userManager = userManager;
            _refreshTokenService = refreshTokenService;
            _usuarioRepo = usuarioRepo;
            _logger = logger;
        }

        private const int DefaultAccessLifetimeMinutes = 15;

        public async Task<AccessTokenResult> GenerateTokenAsync(
            Usuario usuario,
            CancellationToken ct = default)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtOptions:SecurityKey"]!);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
            var claims = await GenerateClaims(usuario);

            var expiresAt = DateTime.UtcNow.AddMinutes(GetAccessLifetimeMinutes());

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = expiresAt,
                Issuer = _configuration["JwtOptions:Issuer"],
                Audience = _configuration["JwtOptions:Audience"]
            };

            var token = handler.CreateToken(tokenDescriptor);

            _logger.LogInformation(
                "Access token JWT gerado para o usuário {UsuarioId} expira em {ExpiresAt}",
                usuario.Id, expiresAt);

            return new AccessTokenResult(handler.WriteToken(token), expiresAt);
        }

        public async Task<RefreshAccessResult> RefreshAccessTokenAsync(
            string rawRefreshToken,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken ct = default)
        {
            var rotate = await _refreshTokenService.RotateAsync(rawRefreshToken, deviceInfo, ipAddress, ct);
            
            if (!rotate.Success || rotate.UserId is null) {
                return RefreshAccessResult.Fail();
            }

            var usuario = await _usuarioRepo.GetForTokenRefreshAsync(rotate.UserId.Value, ct);
            
            if (usuario is null)
            {
                _logger.LogWarning(
                    "Refresh válido mas usuário não encontrado/inativo. UserId={UserId}",
                    rotate.UserId.Value);
                return RefreshAccessResult.Fail();
            }

            var access = await GenerateTokenAsync(usuario, ct);

            return RefreshAccessResult.Ok(
                access.Token,
                access.ExpiresAt,
                rotate.NewRawToken!,
                rotate.NewExpiresAt!.Value);
        }

        private int GetAccessLifetimeMinutes()
        {
            var value = _configuration["JwtOptions:AccessLifetimeMinutes"];
            return int.TryParse(value, out var minutes) && minutes > 0 ? minutes : DefaultAccessLifetimeMinutes;
        }

        public async Task<IEnumerable<Claim>> GenerateClaims(Usuario usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);
            
            var nomeExibicao =
                usuario.Administrador?.Nome
                ?? usuario.Motorista?.NomeReal
                ?? usuario.UserName
                ?? "-";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nomeExibicao!),
                new Claim(ClaimTypes.Email, usuario.Email!),
                new Claim("UserId", usuario.Id.ToString()),
            };

            if (usuario.EmpresaId.HasValue)
            {
                claims.Add(new Claim("EmpresaId", usuario.EmpresaId.Value.ToString()));
            }

            if (usuario.Motorista != null)
            {
                claims.Add(new Claim("MotoristaId", usuario.Motorista.Id.ToString()));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }
    }
}
