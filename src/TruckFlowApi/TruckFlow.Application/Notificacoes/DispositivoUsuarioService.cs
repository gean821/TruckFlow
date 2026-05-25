using Microsoft.Extensions.Logging;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class DispositivoUsuarioService : IDispositivoUsuarioService
    {
        private readonly IDispositivoUsuarioRepositorio _repo;
        private readonly ICurrentUserService _user;
        private readonly ILogger<DispositivoUsuarioService> _logger;

        public DispositivoUsuarioService(
            IDispositivoUsuarioRepositorio repo,
            ICurrentUserService user,
            ILogger<DispositivoUsuarioService> logger)
        {
            _repo = repo;
            _user = user;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            RegistrarDispositivoDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.ExpoPushToken))
            {
                throw new BusinessException("ExpoPushToken obrigatório.");
            }

            var userId = _user.UserId;
            var existente = await _repo.GetByTokenAsync(dto.ExpoPushToken, ct);

            if (existente is not null)
            {
                if (existente.UsuarioId != userId)
                {
                    _logger.LogInformation(
                        "Transferindo dispositivo {DispositivoId} (token {Token}) de {AntigoUsuario} para {NovoUsuario}.",
                        existente.Id,
                        MaskToken(dto.ExpoPushToken),
                        existente.UsuarioId,
                        userId);

                    existente.TransferirPara(userId);
                }
                else
                {
                    existente.RegistrarUso();
                }

                existente.Plataforma = dto.Plataforma;
                existente.AppVersion = dto.AppVersion;

                await _repo.UpdateAsync(existente, ct);
                return;
            }

            var dispositivo = new DispositivoUsuario
            {
                UsuarioId = userId,
                ExpoPushToken = dto.ExpoPushToken,
                Plataforma = dto.Plataforma,
                AppVersion = dto.AppVersion,
                UltimoUsoEm = DateTime.UtcNow,
                Ativo = true,
                CreatedAt = DateTime.UtcNow,
            };

            await _repo.AddAsync(dispositivo, ct);

            _logger.LogInformation(
                "Dispositivo registrado {DispositivoId} usuário {UsuarioId} plataforma {Plataforma}.",
                dispositivo.Id, userId, dto.Plataforma);
        }

        private static string MaskToken(string token)
        {
            if (token.Length <= 12) return "***";
            return $"{token[..6]}...{token[^4..]}";
        }
    }
}