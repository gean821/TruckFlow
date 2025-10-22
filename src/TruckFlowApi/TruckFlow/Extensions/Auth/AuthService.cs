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
using TruckFlow.Application.Entities;
using TruckFlow.Interfaces;

namespace TruckFlow.Application.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        public AuthService (IConfiguration configuration) =>
            _configuration = configuration;
        
        
        public string GenerateToken(Usuario usuario)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtOptions:SecurityKey"]!);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = GenerateClaims(usuario),
                SigningCredentials = credentials,
                Expires = DateTime.UtcNow.AddHours(4),
                Issuer = _configuration["JwtOptions:Issuer"],
                Audience = _configuration["JwtOptions:Audience"]
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public static ClaimsIdentity GenerateClaims(Usuario usuario)
        {
            var ci = new ClaimsIdentity();
            ci.AddClaim(new Claim(ClaimTypes.Name, usuario.UserName!));
            ci.AddClaim(new Claim(ClaimTypes.Email, usuario.Email!));
            ci.AddClaim(new Claim("Telefone", usuario.Motorista!.Telefone.ToString()));
            return ci;
        }
    }
}
