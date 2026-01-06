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

namespace TruckFlow.Application
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IAuthService _authService;
        private readonly AppDbContext _db;

        public UsuarioService(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IAuthService authService,
            AppDbContext db
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authService = authService;
            _db = db;
        }

        public async Task<UserAdminResponseDto> RegisterAdminAsync
            (
                UserAdminRegisterDto dto,
                CancellationToken token = default
            )
        {
            var usuario = new Usuario
            {
                Id = dto.Id,
                UserName = dto.Username,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow,
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
                Nome = dto.Username,
                UsuarioId = usuario.Id,
                Usuario = usuario,
                CreatedAt = DateTime.UtcNow,
                FuncaoAdm = FuncaoAdministrador.Colaborador,
            };

            _db.Administrador.Add(administrador);
            await _db.SaveChangesAsync(token);

            return new UserAdminResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Username = usuario.UserName!,
                CreatedAt = usuario.CreatedAt
            };
        }

        public async Task<LoginAdminResponseDto> LoginAdminAsync(UserAdminLoginDto dto, CancellationToken ct = default)
        {
            var usuario = await _userManager.FindByNameAsync(dto.Username);

            if (usuario!.DeletedAt != null)
            {
                throw new UnauthorizedAccessException("Usuário desativado.");
            }

            if (usuario == null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var token = await _authService.GenerateTokenAsync(usuario, ct);

            return new LoginAdminResponseDto
            {
                Token = token,
                Usuario = usuario
            };
        }

        public Task<List<UserAdminResponseDto>> GetAllAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }


        public Task<UserAdminResponseDto> GetAdminByIdAsync(Guid id, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        public async Task<UserAdminResponseDto> UpdateAdminAsync
            (
                Guid id,
                UserAdminEditDto dto,
                CancellationToken token = default
            )
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuário não encontrado.");

            usuario.Email = dto.Email;
            usuario.UserName = dto.Username;
            usuario.PhoneNumber = dto.Telefone;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);

            return new UserAdminResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Username = usuario.UserName!,
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt
            };

        }
        public async Task DeleteAdminAsync(Guid id, CancellationToken token = default)
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
                Nome = dto.UserName,
                UsuarioId = usuario.Id,
                Usuario = usuario,
                Telefone = usuario.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Motorista.Add(motorista);
            await _db.SaveChangesAsync(token);

            return new UserMotoristaResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Username = usuario.UserName!,
                Telefone = usuario.PhoneNumber,
                CreatedAt = usuario.CreatedAt
            };
        }

        public async Task<LoginMotoristaResponseDto> LoginMotoristaAsync
            (
                UserMotoristaLoginDto dto,
                CancellationToken ct = default
            )
        {
            
            var usuario = await _userManager.FindByNameAsync(dto.Username);

            if (usuario!.DeletedAt != null)
            {
                throw new UnauthorizedAccessException("Usuário desativado.");
            }

            if (usuario == null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                throw new UnauthorizedAccessException("Usuário ou senha inválidos");
            }

            var token = await _authService.GenerateTokenAsync(usuario, ct);

            return new LoginMotoristaResponseDto
            {
                Token = token,
                Usuario = usuario
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

            return new UserMotoristaResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Username = usuario.UserName!,
                Telefone = usuario.PhoneNumber!,
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt
            };
        }

        public async Task<List<UserMotoristaResponseDto>> GetAllMotoristaAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
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

            motorista.Nome = dto.Username;
            motorista.Telefone = dto.Telefone;
            motorista.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(token);

            return new UserMotoristaResponseDto
            {
                Id = usuario.Id,
                Username = usuario.UserName!,
                Email = usuario.Email!,
                Telefone = usuario.PhoneNumber!,
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt
            };
        }

        public async Task DeleteMotoristaAsync(Guid id, CancellationToken token = default)
        {
            var usuario = await _userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Usuario não encontrado.");

            usuario.DeletedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(usuario);
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
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
    }
}
