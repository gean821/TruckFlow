using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application
{
    public class VerificacaoEmailService : IVerificacaoEmailService
    {
        private readonly ICodigoVerificacaoEmailRepositorio _repo;
        private readonly IEmailService _emailService;
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerificacaoEmailService> _logger;

        private const int MaxEnviosPerHora = 3;
        private const int ExpiracaoMinutos = 15;
        private const int TokenExpiracaoMinutos = 5;

        public VerificacaoEmailService(
            ICodigoVerificacaoEmailRepositorio repo,
            IEmailService emailService,
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            ILogger<VerificacaoEmailService> logger)
        {
            _repo = repo;
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnviarCodigoAsync(
            Guid usuarioId,
            FinalidadeVerificacaoEmail finalidade,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            var enviosRecentes = await _repo.ContarEnviosRecentesAsync(
                usuarioId, finalidade, TimeSpan.FromHours(1), token);

            if (enviosRecentes >= MaxEnviosPerHora)
                throw new BusinessException("Limite de envios atingido. Aguarde 1 hora antes de solicitar um novo código.");

            await _repo.InvalidarAnterioresAsync(usuarioId, finalidade, token);

            var codigo = GerarCodigo();

            var entidade = new CodigoVerificacaoEmail
            {
                UsuarioId = usuarioId,
                CodigoHash = HashCodigo(codigo),
                Finalidade = finalidade,
                ExpiraEm = DateTime.UtcNow.AddMinutes(ExpiracaoMinutos),
                CriadoEm = DateTime.UtcNow
            };

            await _repo.AdicionarAsync(entidade, token);
            await _repo.SalvarAsync(token);

            await _emailService.SendAsync(
                usuario.Email!,
                ObterAssunto(finalidade),
                GerarTemplate(finalidade, codigo, usuario.Email!),
                token);

            _logger.LogInformation(
                "Código de verificação enviado para usuário {UsuarioId} finalidade {Finalidade}",
                usuarioId, finalidade);
        }

        public async Task<string> ValidarCodigoAsync(
            Guid usuarioId,
            string codigo,
            FinalidadeVerificacaoEmail finalidade,
            CancellationToken token = default)
        {
            var entidade = await _repo.ObterUltimoAtivoAsync(usuarioId, finalidade, token)
                ?? throw new BusinessException("Código inválido ou expirado.");

            if (entidade.CodigoHash != HashCodigo(codigo))
            {
                entidade.Tentativas++;
                await _repo.SalvarAsync(token);
                throw new BusinessException("Código incorreto.");
            }

            entidade.UsadoEm = DateTime.UtcNow;
            await _repo.SalvarAsync(token);

            _logger.LogInformation(
                "Código validado com sucesso para usuário {UsuarioId} finalidade {Finalidade}",
                usuarioId, finalidade);

            return GerarCodigoToken(usuarioId, finalidade);
        }

        public (Guid UsuarioId, FinalidadeVerificacaoEmail Finalidade) ExtrairCodigoToken(string codigoToken)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["JwtOptions:SecurityKey"]!);
            var handler = new JwtSecurityTokenHandler();

            try
            {
                handler.ValidateToken(codigoToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                    ?? throw new BusinessException("Token de verificação inválido.");

                var finalidadeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Finalidade")?.Value
                    ?? throw new BusinessException("Token de verificação inválido.");

                var tipoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "typ")?.Value;
                if (tipoClaim != "email_code")
                    throw new BusinessException("Token de verificação inválido.");

                return (Guid.Parse(userIdClaim), (FinalidadeVerificacaoEmail)int.Parse(finalidadeClaim));
            }
            catch (BusinessException)
            {
                throw;
            }
            catch
            {
                throw new BusinessException("Token de verificação inválido ou expirado.");
            }
        }

        private string GerarCodigoToken(Guid usuarioId, FinalidadeVerificacaoEmail finalidade)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["JwtOptions:SecurityKey"]!);
            var handler = new JwtSecurityTokenHandler();

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("UserId", usuarioId.ToString()),
                    new Claim("Finalidade", ((int)finalidade).ToString()),
                    new Claim("typ", "email_code")
                }),
                Expires = DateTime.UtcNow.AddMinutes(TokenExpiracaoMinutos),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private static string GerarCodigo()
        {
            var numero = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return numero.ToString("D6");
        }

        private static string HashCodigo(string codigo)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codigo));
            return Convert.ToHexString(hash).ToLower();
        }

        private static string ObterAssunto(FinalidadeVerificacaoEmail finalidade) => finalidade switch
        {
            FinalidadeVerificacaoEmail.CriarConta => "Confirme seu e-mail - TruckFlow",
            FinalidadeVerificacaoEmail.AlterarSenha => "Código para redefinição de senha - TruckFlow",
            FinalidadeVerificacaoEmail.AlterarEmail => "Confirme a alteração do seu e-mail - TruckFlow",
            _ => "Código de verificação - TruckFlow"
        };

        private static string GerarTemplate(FinalidadeVerificacaoEmail finalidade, string codigo, string email)
        {
            var titulo = finalidade switch
            {
                FinalidadeVerificacaoEmail.CriarConta => "Confirme seu e-mail",
                FinalidadeVerificacaoEmail.AlterarSenha => "Redefinição de senha",
                FinalidadeVerificacaoEmail.AlterarEmail => "Alteração de e-mail",
                _ => "Verificação"
            };

            var mensagem = finalidade switch
            {
                FinalidadeVerificacaoEmail.CriarConta => "Use o código abaixo para confirmar seu e-mail e ativar sua conta.",
                FinalidadeVerificacaoEmail.AlterarSenha => "Use o código abaixo para confirmar a redefinição da sua senha.",
                FinalidadeVerificacaoEmail.AlterarEmail => $"Use o código abaixo para confirmar a alteração do e-mail <strong>{email}</strong>.",
                _ => "Use o código abaixo para continuar."
            };

            return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:40px;">
                  <div style="max-width:480px;margin:auto;background:#fff;border-radius:8px;padding:40px;">
                    <h2 style="color:#1a1a1a;">{titulo}</h2>
                    <p style="color:#444;">{mensagem}</p>
                    <div style="text-align:center;margin:32px 0;">
                      <span style="font-size:40px;font-weight:bold;letter-spacing:12px;color:#1a1a1a;">{codigo}</span>
                    </div>
                    <p style="color:#888;font-size:13px;">Este código expira em 15 minutos. Não compartilhe com ninguém.</p>
                    <p style="color:#888;font-size:13px;">Se você não solicitou este código, ignore este e-mail.</p>
                  </div>
                </body>
                </html>
                """;
        }
    }
}
