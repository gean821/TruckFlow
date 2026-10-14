using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

/// <summary>
/// Usuario não é IEmpresaScoped (herda IdentityUser), então o filtro global de tenant não o protege.
/// Estes testes garantem que os métodos de admin por id do UsuarioService restringem à empresa do
/// usuário logado: admin da empresa B não lê nem altera usuário da empresa A.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class UsuarioCrossTenantTests : IntegrationTestBase
{
    public UsuarioCrossTenantTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetAdminByIdAsync_UsuarioDeOutraEmpresa_LancaNotFound()
    {
        var (_, empresaBId, usuarioAId) = await SeedDuasEmpresasComAdminEmAAsync();

        using var db = Fixture.CreateDbContext();
        await using var scope = CriarUsuarioService(db, empresaBId, out var service);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAdminByIdAsync(usuarioAId));
    }

    [Fact]
    public async Task GetAdminByIdAsync_UsuarioDaMesmaEmpresa_RetornaUsuario()
    {
        var (empresaAId, _, usuarioAId) = await SeedDuasEmpresasComAdminEmAAsync();

        using var db = Fixture.CreateDbContext();
        await using var scope = CriarUsuarioService(db, empresaAId, out var service);

        var usuario = await service.GetAdminByIdAsync(usuarioAId);

        Assert.Equal(usuarioAId, usuario.Id);
        Assert.Equal(empresaAId, usuario.EmpresaId);
    }

    [Fact]
    public async Task SetAdminStatusAsync_UsuarioDeOutraEmpresa_LancaNotFoundENaoInativa()
    {
        var (_, empresaBId, usuarioAId) = await SeedDuasEmpresasComAdminEmAAsync();

        using (var db = Fixture.CreateDbContext())
        await using (CriarUsuarioService(db, empresaBId, out var service))
        {
            await Assert.ThrowsAsync<NotFoundException>(() => service.SetAdminStatusAsync(usuarioAId, ativo: false));
        }

        using var assertDb = Fixture.CreateDbContext();
        var usuario = await assertDb.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioAId);
        Assert.Null(usuario.DeletedAt);
    }

    [Fact]
    public async Task UpdateAdminAsync_UsuarioDeOutraEmpresa_LancaNotFoundENaoAltera()
    {
        var (_, empresaBId, usuarioAId) = await SeedDuasEmpresasComAdminEmAAsync();
        string emailOriginal;
        using (var setupDb = Fixture.CreateDbContext())
        {
            emailOriginal = (await setupDb.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioAId)).Email!;
        }

        using (var db = Fixture.CreateDbContext())
        await using (CriarUsuarioService(db, empresaBId, out var service))
        {
            var dto = new UserAdminEditDto { Email = "invasor@empresa-b.teste" };
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAdminAsync(usuarioAId, dto));
        }

        using var assertDb = Fixture.CreateDbContext();
        var usuario = await assertDb.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioAId);
        Assert.Equal(emailOriginal, usuario.Email);
    }

    [Fact]
    public async Task DeleteAdminAsync_UsuarioDeOutraEmpresa_LancaNotFoundENaoExclui()
    {
        var (_, empresaBId, usuarioAId) = await SeedDuasEmpresasComAdminEmAAsync();

        using (var db = Fixture.CreateDbContext())
        await using (CriarUsuarioService(db, empresaBId, out var service))
        {
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAdminAsync(usuarioAId));
        }

        using var assertDb = Fixture.CreateDbContext();
        var usuario = await assertDb.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioAId);
        Assert.Null(usuario.DeletedAt);
    }

    private async Task<(Guid EmpresaAId, Guid EmpresaBId, Guid UsuarioAId)> SeedDuasEmpresasComAdminEmAAsync()
    {
        var empresaA = TestDataSeeder.NovaEmpresa("Empresa A");
        var empresaB = TestDataSeeder.NovaEmpresa("Empresa B");

        var setupEmpresaContext = ServiceFactory.NewEmpresaContext();
        using var tenantScope = setupEmpresaContext.WithTenant(empresaA.Id);
        using var db = Fixture.CreateDbContext(setupEmpresaContext);

        db.Empresa.AddRange(empresaA, empresaB);

        var roleAdmin = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = Roles.Admin,
            NormalizedName = Roles.Admin.ToUpperInvariant(),
        };
        db.Roles.Add(roleAdmin);

        // O seeder só tem builder de motorista; o que importa aqui é EmpresaId + papel Admin.
        var usuarioA = TestDataSeeder.NovoUsuarioMotorista(empresaA.Id);
        db.Usuario.Add(usuarioA);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = usuarioA.Id, RoleId = roleAdmin.Id });

        await db.SaveChangesAsync();
        return (empresaA.Id, empresaB.Id, usuarioA.Id);
    }

    /// <summary>
    /// Monta o UsuarioService com UserManager/RoleManager reais sobre o mesmo AppDbContext do teste,
    /// logado como admin da empresa informada. IAuthService e IRefreshTokenService não são usados
    /// pelos métodos de admin por id, por isso não são montados.
    /// </summary>
    private static ServiceProvider CriarUsuarioService(AppDbContext db, Guid empresaLogadaId, out UsuarioService service)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddIdentityCore<Usuario>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        var provider = services.BuildServiceProvider();

        var currentUser = new FakeCurrentUserService
        {
            UserId = Guid.NewGuid(),
            EmpresaId = empresaLogadaId,
            IsAdmin = true,
            IsMotorista = false,
        };

        service = new UsuarioService(
            provider.GetRequiredService<UserManager<Usuario>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            authService: null!,
            refreshTokenService: null!,
            new EmpresaRepositorio(db),
            db,
            new CurrentUserGuard(currentUser),
            NullLogger<UsuarioService>.Instance);

        return provider;
    }
}
