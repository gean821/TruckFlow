using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class LoginLockoutTests : IntegrationTestBase
{
    private const string SenhaCorreta = "SenhaForte@123";

    public LoginLockoutTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task LoginAdminAsync_ApósCincoTentativasErradas_BloqueiaMesmoComSenhaCorreta()
    {
        var (db, provider, service, _, usuario) = await SetupComAdminRegistradoAsync();
        using (db)
        await using (provider)
        {
            for (var i = 0; i < 5; i++)
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    service.LoginAdminAsync(
                        new UserAdminLoginDto
                        {
                            Login = usuario.UserName!,
                            Password = "SenhaErrada@1",
                        },
                        deviceInfo: null,
                        ipAddress: null
                    )
                );
            }

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAdminAsync(
                    new UserAdminLoginDto { Login = usuario.UserName!, Password = SenhaCorreta },
                    deviceInfo: null,
                    ipAddress: null
                )
            );

            Assert.Contains("bloqueada", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task LoginAdminAsync_LoginBemSucedido_ResetaContadorDeTentativas()
    {
        var (db, provider, service, empresaId, usuario) = await SetupComAdminRegistradoAsync();
        using (db)
        await using (provider)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAdminAsync(
                    new UserAdminLoginDto { Login = usuario.UserName!, Password = "SenhaErrada@1" },
                    deviceInfo: null,
                    ipAddress: null
                )
            );

            var resultado = await service.LoginAdminAsync(
                new UserAdminLoginDto { Login = usuario.UserName!, Password = SenhaCorreta },
                deviceInfo: null,
                ipAddress: null
            );

            Assert.NotNull(resultado.Token);

            for (var i = 0; i < 4; i++)
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    service.LoginAdminAsync(
                        new UserAdminLoginDto
                        {
                            Login = usuario.UserName!,
                            Password = "SenhaErrada@1",
                        },
                        deviceInfo: null,
                        ipAddress: null
                    )
                );
            }

            var resultadoFinal = await service.LoginAdminAsync(
                new UserAdminLoginDto { Login = usuario.UserName!, Password = SenhaCorreta },
                deviceInfo: null,
                ipAddress: null
            );

            Assert.NotNull(resultadoFinal.Token);
        }
    }

    [Fact]
    public async Task RegisterAdminAsync_SenhaFraca_LancaBusinessExceptionComMensagemDoIdentity()
    {
        var (db, provider, service, empresaId) = await SetupServicoSemUsuarioAsync();
        using (db)
        await using (provider)
        {
            var dto = new UserAdminRegisterDto
            {
                Email = "fraca@teste.com",
                Password = "123456",
                Username = "usuario-fraco",
                NomeReal = "Usuário Fraco",
                Telefone = "41999999999",
                EmpresaId = empresaId,
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                service.RegisterAdminAsync(dto)
            );

            Assert.Contains("least", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<(
        AppDbContext Db,
        ServiceProvider Provider,
        UsuarioService Service,
        Guid EmpresaId,
        Usuario Usuario
    )> SetupComAdminRegistradoAsync()
    {
        var (db, provider, service, empresaId) = await SetupServicoSemUsuarioAsync();

        var dto = new UserAdminRegisterDto
        {
            Email = $"{Guid.NewGuid():N}@teste.com",
            Password = SenhaCorreta,
            Username = $"admin-{Guid.NewGuid():N}",
            NomeReal = "Admin Teste",
            Telefone = "41999999999",
            EmpresaId = empresaId,
        };

        await service.RegisterAdminAsync(dto);

        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Email == dto.Email);

        return (db, provider, service, empresaId, usuario);
    }

    private async Task<(
        AppDbContext Db,
        ServiceProvider Provider,
        UsuarioService Service,
        Guid EmpresaId
    )> SetupServicoSemUsuarioAsync()
    {
        var empresa = TestDataSeeder.NovaEmpresa("Empresa Lockout Teste");

        var empresaContext = ServiceFactory.NewEmpresaContext();
        using (empresaContext.WithTenant(empresa.Id))
        using (var seedDb = Fixture.CreateDbContext(empresaContext))
        {
            seedDb.Empresa.Add(empresa);
            await seedDb.SaveChangesAsync();
        }

        var db = Fixture.CreateDbContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services
            .AddIdentity<Usuario, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();

        var currentUser = new FakeCurrentUserService
        {
            UserId = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            IsAdmin = true,
            IsMotorista = false,
        };

        var service = new UsuarioService(
            provider.GetRequiredService<UserManager<Usuario>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            new FakeAuthService(),
            new FakeRefreshTokenService(),
            new EmpresaRepositorio(db),
            db,
            new CurrentUserGuard(currentUser),
            NullLogger<UsuarioService>.Instance
        );

        return (db, provider, service, empresa.Id);
    }
}
