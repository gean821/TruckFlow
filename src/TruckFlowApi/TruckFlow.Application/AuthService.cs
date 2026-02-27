using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace TruckFlow.Application
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<Usuario> _userManager;

        public AuthService(
            IConfiguration configuration,
            UserManager<Usuario> userManager
            )
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(Usuario usuario, CancellationToken ct = default)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtOptions:SecurityKey"]!);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
            var claims = await GenerateClaims(usuario);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = DateTime.UtcNow.AddHours(4),
                Issuer = _configuration["JwtOptions:Issuer"],
                Audience = _configuration["JwtOptions:Audience"]
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public async Task<IEnumerable<Claim>> GenerateClaims(Usuario usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.UserName!),
                new Claim(ClaimTypes.Email, usuario.Email!),
                new Claim("UserId", usuario.Id.ToString()),
                new Claim("EmpresaId", usuario.EmpresaId.ToString())
            };

            if (usuario.Motorista != null)
            {
                claims.Add(new Claim("MotoristaId", usuario.Motorista.Id.ToString()));
                claims.Add(new Claim("NomeReal", usuario.Motorista.NomeReal ?? ""));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }
    }
}
