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
            FinalidadeVerificacaoEmail.AlterarSenha => "Código para redefinição de senha - TruckFlow",
            FinalidadeVerificacaoEmail.AlterarEmail => "Confirme a alteração do seu e-mail - TruckFlow",
            _ => "Código de verificação - TruckFlow"
        };

        private static string GerarTemplate(FinalidadeVerificacaoEmail finalidade, string codigo, string email)
        {
            var titulo = finalidade switch
            {
                FinalidadeVerificacaoEmail.AlterarSenha => "Redefinição de senha",
                FinalidadeVerificacaoEmail.AlterarEmail => "Alteração de e-mail",
                _ => "Verificação"
            };

            var icone = finalidade switch
            {
                FinalidadeVerificacaoEmail.AlterarSenha => "🔐",
                FinalidadeVerificacaoEmail.AlterarEmail => "📧",
                _ => "🔑"
            };

            var mensagem = finalidade switch
            {
                FinalidadeVerificacaoEmail.AlterarSenha => "Use o código abaixo para confirmar a redefinição da sua senha.",
                FinalidadeVerificacaoEmail.AlterarEmail => $"Use o código abaixo para confirmar a alteração para o e-mail <strong style=\"color:#0d3f6e;\">{email}</strong>.",
                _ => "Use o código abaixo para continuar."
            };
            return $"""
                <!DOCTYPE html>
                <html lang="pt-BR">
                <head>
                  <meta charset="UTF-8">
                  <meta name="viewport" content="width=device-width,initial-scale=1.0">
                </head>
                <body style="margin:0;padding:0;background-color:#eef2f7;font-family:'Segoe UI',Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#eef2f7;padding:40px 16px;">
                    <tr>
                      <td align="center">
                        <table width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:520px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(10,46,82,0.14);">

                          <tr>
                            <td style="background:#ffffff;padding:28px 40px 24px;text-align:center;border-bottom:1px solid #e8f0f8;">
                              <img src="https://i.imgur.com/jtkMbRi.png" alt="TruckFlow" width="200" style="display:block;margin:0 auto;border:0;outline:none;" />
                            </td>
                          </tr>

                          <tr>
                            <td style="background:linear-gradient(135deg,#0a2e52 0%,#0d3f6e 60%,#195fa0 100%);padding:18px 40px;text-align:center;">
                              <p style="margin:0;font-size:16px;font-weight:600;color:#ffffff;letter-spacing:0.3px;">{titulo}</p>
                            </td>
                          </tr>

                          <tr>
                            <td style="padding:36px 40px 28px;">
                              <p style="margin:0 0 32px;font-size:15px;color:#4a5568;line-height:1.75;text-align:center;">{mensagem}</p>
                              <table width="100%" cellpadding="0" cellspacing="0" border="0">
                                <tr>
                                  <td align="center">
                                    <table cellpadding="0" cellspacing="0" border="0">
                                      <tr>
                                        <td style="background:linear-gradient(135deg,#0a2e52,#195fa0);border-radius:14px;padding:26px 44px;text-align:center;">
                                          <div style="font-size:11px;font-weight:600;color:rgba(255,255,255,0.65);letter-spacing:3px;text-transform:uppercase;margin-bottom:12px;">Código de verificação</div>
                                          <div style="font-size:46px;font-weight:800;letter-spacing:14px;color:#ffffff;font-family:'Courier New',monospace;">{codigo}</div>
                                        </td>
                                      </tr>
                                    </table>
                                  </td>
                                </tr>
                              </table>
                              <p style="margin:22px 0 0;text-align:center;font-size:13px;color:#718096;">
                                ⏱️ Este código expira em <strong style="color:#0d3f6e;">15 minutos</strong>
                              </p>
                            </td>
                          </tr>

                          <tr>
                            <td style="padding:0 40px 32px;">
                              <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#fff8e1;border-left:4px solid #f59e0b;border-radius:0 8px 8px 0;">
                                <tr>
                                  <td style="padding:14px 18px;">
                                    <p style="margin:0;font-size:13px;color:#78350f;line-height:1.65;">
                                      <strong>⚠️ Atenção:</strong> Não compartilhe este código com ninguém. Se você não solicitou, ignore este e-mail.
                                    </p>
                                  </td>
                                </tr>
                              </table>
                            </td>
                          </tr>

                          <tr>
                            <td style="background:#f8fafc;border-top:1px solid #e2e8f0;padding:20px 40px;text-align:center;">
                              <p style="margin:0;font-size:12px;color:#94a3b8;line-height:1.75;">
                                Enviado automaticamente pelo sistema <strong style="color:#0d3f6e;">TruckFlow</strong><br>
                                Não responda a este e-mail.
                              </p>
                            </td>
                          </tr>

                          <tr>
                            <td style="height:5px;background:linear-gradient(90deg,#0a2e52,#195fa0);"></td>
                          </tr>

                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}

