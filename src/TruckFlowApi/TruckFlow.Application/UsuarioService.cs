using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly AppDbContext _db;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IAuthService authService,
            IRefreshTokenService refreshTokenService,
            IEmpresaRepositorio empresaRepo,
            AppDbContext db,
            ILogger<UsuarioService> logger
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authService = authService;
            _refreshTokenService = refreshTokenService;
            _db = db;
            _empresaRepo = empresaRepo;
            _logger = logger;
        }
        public async Task<UserAdminResponseDto> RegisterAdminAsync
            (
                UserAdminRegisterDto dto,
                CancellationToken token = default
            )
        {
            var empresa = await _empresaRepo.GetById(dto.EmpresaId, token);

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                UserName = dto.Username,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow,
                PhoneNumber = dto.Telefone,
                EmpresaId = dto.EmpresaId
            };

            var usuarioCriado = await _userManager.CreateAsync(usuario, dto.Password);

            if (!usuarioCriado.Succeeded)
            {
                _logger.LogWarning(
                    "Falha ao registrar admin: {Errors}",
                    string.Join(" | ", usuarioCriado.Errors.Select(e => e.Description)));
                throw new Exception(string.Join(" | ", usuarioCriado.Errors.Select(e => e.Description)));
            }

            var role = string.IsNullOrWhiteSpace(dto.Role) ? Roles.Admin : dto.Role.Trim();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }

            await _userManager.AddToRoleAsync(usuario, role);

            if (role == Roles.Admin)
            {
                var administrador = new Administrador
                {
                    UserName = usuario.UserName,
                    Nome = dto.NomeReal,
                    UsuarioId = usuario.Id,
                    Usuario = usuario,
                    CreatedAt = usuario.CreatedAt,
                };

                _db.Administrador.Add(administrador);
            }

            await _db.SaveChangesAsync(token);

            _logger.LogInformation(
                "Usuário registrado: {UsuarioId} role {Role} (empresa {EmpresaId})",
                usuario.Id, role, dto.EmpresaId);

            return await MapUsuarioAsync(usuario);
        }

        public async Task<LoginAdminResponseDto> LoginAdminAsync(
            UserAdminLoginDto dto,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken ct = default
            )
        {
            Usuario? usuario;

            if (dto.Login.Contains('@'))
            {
                usuario = await _userManager.FindByEmailAsync(dto.Login);
            }
            else
            {
                usuario = await _userManager.FindByNameAsync(dto.Login);
            }

            if (usuario == null || usuario.DeletedAt != null)
            {
                _logger.LogWarning(
                    "Falha de login admin: usuário não encontrado ou inativo (login {Login})",
                    dto.Login);
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            if (!await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                _logger.LogWarning(
                    "Falha de login admin: senha inválida para {UsuarioId}",
                    usuario.Id);
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var usuarioComAdmin = await _db.Users
                .Include(u => u.Administrador)
                .FirstOrDefaultAsync(u => u.Id == usuario.Id, ct);

            var accessToken = await _authService.GenerateTokenAsync(usuarioComAdmin!, ct);
            var refresh = await _refreshTokenService.IssueAsync(
                usuarioComAdmin!.Id,
                usuarioComAdmin.EmpresaId,
                deviceInfo,
                ipAddress,
                ct: ct);

            _logger.LogInformation(
                "Login admin realizado: {UsuarioId} (empresa {EmpresaId})",
                usuarioComAdmin.Id, usuarioComAdmin.EmpresaId);

            return new LoginAdminResponseDto
            {
                Token = accessToken.Token,
                TokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refresh.RawToken,
                RefreshTokenExpiresAt = refresh.ExpiresAt,
                Usuario = await MapUsuarioAsync(usuarioComAdmin)
            };
        }

        public async Task<PagedResponse<UserAdminResponseDto>> GetPagedUsers(
            UsuarioListQueryDto query,
            Guid empresaId,
            CancellationToken token = default)
        {
            var dbQuery = _db.Users
                .AsNoTracking()
                .Include(u => u.Administrador)
                .Include(u => u.Empresa)
                .Where(u => u.EmpresaId == empresaId);
                //.Where(u => u.Administrador != null);

            if (query.Status == "active")
            {
                dbQuery = dbQuery.Where(u => u.DeletedAt == null);
            }

            else if (query.Status == "inactive")
            {
                dbQuery = dbQuery.Where(u => u.DeletedAt != null);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToUpper();
                dbQuery = dbQuery.Where(u =>
                    (u.NormalizedUserName != null && u.NormalizedUserName.Contains(search)) ||
                    (u.NormalizedEmail != null && u.NormalizedEmail.Contains(search)) ||
                    (u.Administrador != null && u.Administrador.Nome.ToUpper().Contains(search)));
            }

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

            var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

            var totalCount = await dbQuery.CountAsync(token);

            var users = await dbQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            var items = new List<UserAdminResponseDto>(users.Count);
            
            foreach (var u in users)
            {
                items.Add(await MapUsuarioAsync(u));
            }

            return new PagedResponse<UserAdminResponseDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<List<string>> GetRolesAsync(CancellationToken token = default)
        {
            return await _roleManager.Roles
                .AsNoTracking()
                .Where(r => r.Name != null)
                .Select(r => r.Name!)
                .OrderBy(r => r)
                .ToListAsync(token);
        }

        public async Task<UserAdminResponseDto> SetAdminStatusAsync(
            Guid id,
            bool ativo,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            usuario.DeletedAt = ativo ? null : DateTime.UtcNow;
            usuario.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(usuario);

            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "Falha ao alterar status do usuário {UsuarioId}: {Errors}",
                    id, erros);
                throw new BadRequestException($"Erro ao alterar status: {erros}");
            }

            _logger.LogInformation(
                "Status do administrador {UsuarioId} alterado para {Status} (empresa {EmpresaId})",
                id, ativo ? "ativo" : "inativo", usuario.EmpresaId);

            var atualizado = await _db.Users
                .AsNoTracking()
                .Include(u => u.Administrador)
                .Include(u => u.Empresa)
                .FirstAsync(u => u.Id == id, token);

            return await MapUsuarioAsync(atualizado);
        }
        public async Task<UserAdminResponseDto> GetAdminByIdAsync(
            Guid id,
            CancellationToken token = default
            )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                            ?? throw new NotFoundException("Usuário não encontrado.");

            return await MapUsuarioAsync(usuario);
        }
        public async Task<UserAdminResponseDto> UpdateAdminAsync(
            Guid id,
            UserAdminEditDto dto,
            CancellationToken token = default
        )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            var adm = await _db.Administrador
                .FirstOrDefaultAsync(a => a.UsuarioId == id, token);

            await ApplyPatch(adm, usuario, dto, token);

            var result = await _userManager.UpdateAsync(usuario);

            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "Falha ao atualizar admin {UsuarioId}: {Errors}",
                    id, erros);
                throw new BadRequestException($"Erro ao atualizar usuário: {erros}");
            }

            await _db.SaveChangesAsync(token);

            var usuarioAtualizado = await _db.Users
                .Include(u => u.Administrador)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.Id == id, token)
                ?? throw new NotFoundException("Usuário não encontrado após atualização.");

            _logger.LogInformation(
                "Administrador atualizado: {UsuarioId} (empresa {EmpresaId})",
                id, usuarioAtualizado.EmpresaId);

            return await MapUsuarioAsync(usuarioAtualizado);
        }

        public async Task DeleteAdminAsync(
            Guid id,
            CancellationToken token = default
            )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuario não encontrado.");

            usuario.DeletedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);

            _logger.LogInformation(
                "Administrador excluído: {UsuarioId} (empresa {EmpresaId})",
                id, usuario.EmpresaId);
        }

        public async Task<UserMotoristaResponseDto> RegisterMotoristaAsync
            (
                UserMotoristaRegisterDto dto,
                CancellationToken token = default
            )
        {
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.Telefone,
                CreatedAt = DateTime.UtcNow,
            };

            var usuarioCriado = await _userManager.CreateAsync(usuario, dto.Password);

            if (!usuarioCriado.Succeeded)
            {
                _logger.LogWarning(
                    "Falha ao registrar motorista: {Errors}",
                    string.Join(" | ", usuarioCriado.Errors.Select(e => e.Description)));
                throw new Exception(string.Join(" | ", usuarioCriado.Errors.Select(e => e.Description)));
            }

            if (!await _roleManager.RoleExistsAsync(Roles.Motorista))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Motorista));
            }

            await _userManager.AddToRoleAsync(usuario, Roles.Motorista);

            var motorista = new Motorista
            {
                NomeReal = dto.NomeReal,
                Username = dto.UserName,
                UsuarioId = usuario.Id,
                Usuario = usuario,
                Telefone = usuario.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Motorista.Add(motorista);
            await _db.SaveChangesAsync(token);

            _logger.LogInformation(
                "Motorista registrado: {UsuarioId} (motorista {MotoristaId})",
                usuario.Id, motorista.Id);

            return MapMotoristaAsync(usuario);
        }

        public async Task<LoginMotoristaResponseDto> LoginMotoristaAsync
            (
                UserMotoristaLoginDto dto,
                string? deviceInfo,
                string? ipAddress,
                CancellationToken ct = default
            )
        {
            Usuario? usuario;

            if (dto.Login.Contains('@'))
            {
                usuario = await _userManager.FindByEmailAsync(dto.Login);
            }
            else
            {
                usuario = await _userManager.FindByNameAsync(dto.Login);
            }

            if (usuario == null || usuario.DeletedAt != null)
            {
                _logger.LogWarning(
                    "Falha de login motorista: usuário não encontrado ou inativo (login {Login})",
                    dto.Login);
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            if (!await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                _logger.LogWarning(
                    "Falha de login motorista: senha inválida para {UsuarioId}",
                    usuario.Id);
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var usuarioComMotorista = await _db.Users
                .Include(u => u.Motorista)
                .FirstOrDefaultAsync(u => u.Id == usuario.Id, ct);

            var accessToken = await _authService.GenerateTokenAsync(usuarioComMotorista!, ct);
            var refresh = await _refreshTokenService.IssueAsync(
                usuarioComMotorista!.Id, usuarioComMotorista.EmpresaId, deviceInfo, ipAddress, ct: ct);

            _logger.LogInformation(
                "Login motorista realizado: {UsuarioId}",
                usuario.Id);

            return new LoginMotoristaResponseDto
            {
                Token = accessToken.Token,
                TokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refresh.RawToken,
                RefreshTokenExpiresAt = refresh.ExpiresAt,
                Usuario = MapMotoristaAsync(usuarioComMotorista)
            };
        }

        public async Task<UserMotoristaResponseDto> GetMotoristaByIdAsync
            (
                Guid id,
                CancellationToken token
            )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuario não encontrado.");

            return MapMotoristaAsync(usuario);
        }

        public async Task<UserMotoristaResponseDto> UpdateMotoristaAsync(
            Guid id,
            UserMotoristaUpdateDto dto,
            CancellationToken token = default
            )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuário não encontrado");

            var motorista = await _db.Motorista
                .FirstOrDefaultAsync(m => m.UsuarioId == id, token)
                ?? throw new NotFoundException("Motorista não encontrado");

            await ApplyPatchMotorista(motorista, usuario, dto, token);

            var result = await _userManager.UpdateAsync(usuario);
            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Erro ao atualizar usuário: {erros}");
            }
            await _db.SaveChangesAsync(token);

            _logger.LogInformation("Motorista atualizado: {UsuarioId}", id);

            return MapMotoristaAsync(usuario);
        }

        private async Task ApplyPatch(
                Administrador? adm,
                Usuario user,
                UserAdminEditDto dto,
                CancellationToken? token
            )
        {
            if (dto.Username is not null)
            {
                if (adm is not null) adm.UserName = dto.Username;
                user.UserName = dto.Username;
                user.NormalizedUserName = dto.Username.ToUpperInvariant();
            }

            if (dto.Email is not null)
            {
                user.Email = dto.Email;
                user.NormalizedEmail = dto.Email.ToUpperInvariant();
            }

            if (dto.Telefone is not null)
                user.PhoneNumber = dto.Telefone;

            if (dto.Password is not null)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.Password);

                if (!result.Succeeded)
                {
                    var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new BadRequestException($"Senha inválida: {erros}");
                }
            }

            if (adm is not null) adm.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
        }

        private async Task ApplyPatchMotorista(
            Motorista motorista,
            Usuario usuario,
            UserMotoristaUpdateDto dto,
            CancellationToken token
            )
        {
            if (dto.Username is not null)
            {
                motorista.Username = dto.Username;
                usuario.UserName = dto.Username;
                usuario.NormalizedUserName = dto.Username.ToUpperInvariant();
            }

            if (dto.NomeReal is not null)
                motorista.NomeReal = dto.NomeReal;

            if (dto.Email is not null)
            {
                usuario.Email = dto.Email;
                usuario.NormalizedEmail = dto.Email.ToUpperInvariant();
            }

            if (dto.Telefone is not null)
            {
                motorista.Telefone = dto.Telefone;
                usuario.PhoneNumber = dto.Telefone;
            }

            if (dto.Password is not null)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var result = await _userManager.ResetPasswordAsync(usuario, resetToken, dto.Password);
                if (!result.Succeeded)
                {
                    var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                        throw new BadRequestException($"Senha inválida: {erros}");
                }
            }

            motorista.UpdatedAt = DateTime.UtcNow;
            usuario.UpdatedAt = DateTime.UtcNow;
        }

        public async Task DeleteMotoristaAsync(
            Guid id,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuario não encontrado.");

            usuario.DeletedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);

            _logger.LogInformation(
                "Motorista excluído: {UsuarioId}",
                id);
        }

        public async Task ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword)
        {
            var usuario = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado");

            var result = await _userManager.ChangePasswordAsync(
                usuario,
                currentPassword,
                newPassword
            );

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Falha ao alterar senha do usuário {UsuarioId}: {Errors}",
                    userId, string.Join(" | ", result.Errors.Select(e => e.Description)));
                throw new Exception(string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation(
                "Senha alterada para o usuário {UsuarioId}",
                userId);
        }

        public async Task AlterarSenhaComCodigoAsync(
            Guid usuarioId,
            string novaSenha,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var result = await _userManager.ResetPasswordAsync(usuario, resetToken, novaSenha);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(e => e.Description)));

            _logger.LogInformation("Senha alterada via código para usuário {UsuarioId}", usuarioId);
        }

        public async Task AlterarEmailComCodigoAsync(
            Guid usuarioId,
            string novoEmail,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            var emailToken = await _userManager.GenerateChangeEmailTokenAsync(usuario, novoEmail);
            var result = await _userManager.ChangeEmailAsync(usuario, novoEmail, emailToken);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(e => e.Description)));

            usuario.NormalizedEmail = novoEmail.ToUpperInvariant();
            usuario.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(usuario);

            _logger.LogInformation("Email alterado via código para usuário {UsuarioId}", usuarioId);
        }

        public async Task ConfirmarContaAsync(
            Guid usuarioId,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            usuario.EmailConfirmed = true;
            usuario.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(usuario);

            _logger.LogInformation("Conta confirmada via código para usuário {UsuarioId}", usuarioId);
        }

        public async Task AtualizarPerfilAsync(
            Guid usuarioId, 
            AtualizarPerfilAdminDto dto, 
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            if (dto.Username is not null)
            {
                usuario.UserName = dto.Username;
                usuario.NormalizedUserName = dto.Username.ToUpperInvariant();
            }

            if (dto.Telefone is not null)
                usuario.PhoneNumber = dto.Telefone;

            usuario.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(usuario);

            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Erro ao atualizar perfil: {erros}");
            }
        }

        private async Task<UserAdminResponseDto> MapUsuarioAsync(Usuario usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);

            return new UserAdminResponseDto
            {
                Id = usuario.Id,
                Username = usuario.UserName!,
                Email = usuario.Email!,
                Role = roles.First() ?? "User",
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt,
                DeletedAt = usuario.DeletedAt,
                NomeReal = usuario?.Administrador?.Nome,
                Empresa = usuario?.Empresa?.NomeFantasia,
                EmpresaId = usuario?.EmpresaId
            };
        }

            private static UserMotoristaResponseDto MapMotoristaAsync(Usuario usuario)
            {

            return new UserMotoristaResponseDto
            {
                Id = usuario.Id,
                MotoristaId = usuario.Motorista!.Id,
                Username = usuario.UserName!,
                Email = usuario.Email!,
                NomeReal = usuario.Motorista?.NomeReal,
                Telefone = usuario.PhoneNumber!,
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt,
            };
        }
    }
}

