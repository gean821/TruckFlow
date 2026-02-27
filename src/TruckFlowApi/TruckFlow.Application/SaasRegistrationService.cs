using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class SaaSRegistrationService : ISaaSRegistrationService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IAuthService _authService;
        private readonly IEmpresaRepositorio _empresaRepo;

        public SaaSRegistrationService(
            AppDbContext db,
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IAuthService authService,
            IEmpresaRepositorio empresaRepo
            )
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _authService = authService;
            _empresaRepo = empresaRepo;
        }

        public async Task<LoginAdminResponseDto> RegisterAsync(
            RegisterEmpresaAdminDto dto,
            CancellationToken token = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(token);

            try
            {
                var empresaExistente = await _empresaRepo.GetByCnpj(dto.Cnpj, token);

                if (empresaExistente != null)
                {
                    throw new BusinessException("Empresa já cadastrada.");
                }

                var empresa = new Empresa
                {
                    Id = Guid.NewGuid(),
                    RazaoSocial = dto.RazaoSocial,
                    NomeFantasia = dto.NomeFantasia,
                    Cnpj = NormalizeCnpj(dto.Cnpj),
                    Email = dto.EmailEmpresa,
                    Ativa = true,
                    CreatedAt = DateTime.UtcNow,
                    Cidade = dto.Cidade,
                    Bairro = dto.Bairro,
                    Cep = dto.Cep,
                    Estado = dto.Estado,
                    Logradouro = dto.Logradouro,
                    Numero = dto.Numero
                };

                await _empresaRepo.CreateEmpresa(empresa, token);

                var usuario = new Usuario
                {
                    Id = Guid.NewGuid(),
                    UserName = dto.Username,
                    Email = dto.EmailAdmin,
                    PhoneNumber = dto.Telefone,
                    EmpresaId = empresa.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(usuario, dto.Password);

                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(" | ", result.Errors.Select(e => e.Description)));
                }

                if (!await _roleManager.RoleExistsAsync(Roles.Admin))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
                }

                await _userManager.AddToRoleAsync(usuario, Roles.Admin);

                var administrador = new Administrador
                {
                    UsuarioId = usuario.Id,
                    Nome = dto.NomeAdmin,
                    FuncaoAdm = FuncaoAdministrador.Gerente,
                    UserName = dto.Username,
                    Usuario = usuario,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Administrador.AddAsync(administrador, token);

                await _db.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                var jwt = await _authService.GenerateTokenAsync(usuario, token);

                return new LoginAdminResponseDto
                {
                    Token = jwt,
                    Usuario = new UserAdminResponseDto
                    {
                        Id = usuario.Id,
                        Username = usuario.UserName!,
                        Email = usuario.Email!,
                        Role = Roles.Admin,
                        NomeReal = dto.NomeAdmin,
                        Empresa = empresa.NomeFantasia,
                        EmpresaId = empresa.Id,
                        CreatedAt = usuario.CreatedAt
                    }
                };
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        private static string NormalizeCnpj(string cnpj)
            => new string(cnpj.Where(char.IsDigit).ToArray());
    }
}
