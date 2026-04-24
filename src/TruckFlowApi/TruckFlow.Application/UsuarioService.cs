using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
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
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly AppDbContext _db;

        public UsuarioService(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IAuthService authService,
            IEmpresaRepositorio empresaRepo,
            AppDbContext db
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authService = authService;
            _db = db;
            _empresaRepo = empresaRepo;
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
                Empresa = empresa,
                EmpresaId = dto.EmpresaId
            };

            var usuarioCriado = await _userManager.CreateAsync(usuario, dto.Password);

            if (!usuarioCriado.Succeeded)
            {
                throw new Exception(string.Join(" | ", usuarioCriado.Errors.Select(e => e.Description)));
            }

            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
            }

            await _userManager.AddToRoleAsync(usuario, Roles.Admin);

            var administrador = new Administrador
            {
                UserName = usuario.UserName,
                Nome = dto.NomeReal,
                UsuarioId = usuario.Id,
                Usuario = usuario,
                CreatedAt = usuario.CreatedAt,
                FuncaoAdm = FuncaoAdministrador.Colaborador,
            };

            _db.Administrador.Add(administrador);
            await _db.SaveChangesAsync(token);

            return await MapUsuarioAsync(usuario);
        }

        public async Task<LoginAdminResponseDto> LoginAdminAsync(
            UserAdminLoginDto dto,
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
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            if (!await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var token = await _authService.GenerateTokenAsync(usuario, ct);

            return new LoginAdminResponseDto
            {
                Token = token,
                Usuario = await MapUsuarioAsync(usuario)
            };
        }

        public Task<List<UserAdminResponseDto>> GetAllAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
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
                .FirstOrDefaultAsync(a => a.UsuarioId == id, token)
                ?? throw new NotFoundException("Administrador não encontrado.");

            await ApplyPatch(adm, usuario, dto, token);

            var result = await _userManager.UpdateAsync(usuario);

            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Erro ao atualizar usuário: {erros}");
            }

            await _db.SaveChangesAsync(token);

            var usuarioAtualizado = await _db.Users
                .Include(u => u.Administrador)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.Id == id, token)
                ?? throw new NotFoundException("Usuário não encontrado após atualização.");

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

            return await MapMotoristaAsync(usuario);
        }

        public async Task<LoginMotoristaResponseDto> LoginMotoristaAsync
            (
                UserMotoristaLoginDto dto,
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
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            if (!await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var usuarioComMotorista = await _db.Users
                .Include(u => u.Motorista) 
                .FirstOrDefaultAsync(u => u.Id == usuario.Id, ct);

            var token = await _authService.GenerateTokenAsync(usuarioComMotorista!, ct);
            
            return new LoginMotoristaResponseDto
            {
                Token = token,
                Usuario = await MapMotoristaAsync(usuarioComMotorista!)
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

            return await MapMotoristaAsync(usuario);
        }

        public async Task<UserMotoristaResponseDto> UpdateMotoristaAsync
            (
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

            usuario.UserName = dto.Username;
            usuario.Email = dto.Email;
            usuario.PhoneNumber = dto.Telefone;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);

            motorista.Username = dto.Username;
            motorista.NomeReal = dto.NomeReal;
            motorista.Telefone = dto.Telefone;
            motorista.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(token);

            return await MapMotoristaAsync(usuario);
        }

        private async Task ApplyPatch(
                Administrador adm,
                Usuario user,
                UserAdminEditDto dto,
                CancellationToken token
            )
        {
            if (dto.Username is not null)
            {
                adm.UserName = dto.Username;
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

            adm.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
        }

        public async Task DeleteMotoristaAsync(
            Guid id,
            CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuario não encontrado.");

            usuario.DeletedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);
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
                throw new Exception(string.Join(" | ", result.Errors.Select(e => e.Description)));
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

            private async Task<UserMotoristaResponseDto> MapMotoristaAsync(Usuario usuario)
            {
            var roles = await _userManager.GetRolesAsync(usuario);

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

