using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TruckFlow.Domain.Entities;
using TruckFlow.Extensions.Auth;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class SegurancaTenantSessaoHttpTests : IntegrationTestBase
{
    private const string Senha = "SenhaForte@123";
    private const string EmailDev = "dev@truckflow.test";

    public SegurancaTenantSessaoHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task CadeiaDeTakeover_AdminDeEmpresaPropriaNaoEnxergaNemCriaUsuarioEmOutraEmpresa()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var aurora = await RegistrarEmpresaAsync(client, "Aurora");
        var atacante = await RegistrarEmpresaAsync(client, "Atacante");

        var lista = await client.SendAsync(Autenticado(HttpMethod.Get, "/v1/Empresa", atacante.Token));
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var ids = (await lista.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid())
            .ToList();
        Assert.Equal([atacante.EmpresaId], ids);

        var getAurora = await client.SendAsync(Autenticado(HttpMethod.Get, $"/v1/Empresa/{aurora.EmpresaId}", atacante.Token));
        Assert.Equal(HttpStatusCode.NotFound, getAurora.StatusCode);

        var patchAurora = Autenticado(HttpMethod.Patch, $"/v1/Empresa/{aurora.EmpresaId}", atacante.Token);
        patchAurora.Content = JsonContent.Create(new { nomeFantasia = "Hackeada" });
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(patchAurora)).StatusCode);

        var deleteAurora = await client.SendAsync(Autenticado(HttpMethod.Delete, $"/v1/Empresa/{aurora.EmpresaId}", atacante.Token));
        Assert.Equal(HttpStatusCode.Forbidden, deleteAurora.StatusCode);

        var registerAnonimo = await client.PostAsJsonAsync("/v1/AuthAdmin/register", NovoUsuario(aurora.EmpresaId, "intruso-anon"));
        Assert.Equal(HttpStatusCode.Unauthorized, registerAnonimo.StatusCode);

        var registerComTokenDoAtacante = Autenticado(HttpMethod.Post, "/v1/AuthAdmin/register", atacante.Token);
        registerComTokenDoAtacante.Content = JsonContent.Create(NovoUsuario(aurora.EmpresaId, "intruso-admin"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(registerComTokenDoAtacante)).StatusCode);

        using var db = Fixture.CreateDbContext();
        var usuariosAurora = await db.Users.Where(u => u.EmpresaId == aurora.EmpresaId).Select(u => u.UserName).ToListAsync();
        Assert.Equal(["admin-aurora"], usuariosAurora);
        var empresaAurora = await db.Empresa.AsNoTracking().SingleAsync(e => e.Id == aurora.EmpresaId);
        Assert.Equal("Aurora", empresaAurora.NomeFantasia);
        Assert.True(empresaAurora.Ativa);
    }

    [Fact]
    public async Task AdminContinuaLendoEEditandoAPropriaEmpresa()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var empresa = await RegistrarEmpresaAsync(client, "Propria");

        var get = await client.SendAsync(Autenticado(HttpMethod.Get, $"/v1/Empresa/{empresa.EmpresaId}", empresa.Token));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var patch = Autenticado(HttpMethod.Patch, $"/v1/Empresa/{empresa.EmpresaId}", empresa.Token);
        patch.Content = JsonContent.Create(new { nomeFantasia = "Propria Editada" });
        var resposta = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Propria Editada", (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nomeFantasia").GetString());
    }

    [Fact]
    public async Task Desenvolvedor_CriaUsuarioEmQualquerEmpresa_SoComPapelDaListaFixa()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString, EmailDev);
        using var client = factory.CreateApiClient();

        var aurora = await RegistrarEmpresaAsync(client, "Aurora");
        var tokenDev = await CriarDesenvolvedorAsync(factory, client);

        var lista = await client.SendAsync(Autenticado(HttpMethod.Get, "/v1/Empresa", tokenDev));
        Assert.Equal(2, (await lista.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());

        var valido = Autenticado(HttpMethod.Post, "/v1/AuthAdmin/register", tokenDev);
        valido.Content = JsonContent.Create(NovoUsuario(aurora.EmpresaId, "portaria-aurora", "portaria"));
        var respostaValida = await client.SendAsync(valido);
        Assert.Equal(HttpStatusCode.OK, respostaValida.StatusCode);
        Assert.Equal(Roles.Portaria, (await respostaValida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        foreach (var papelInvalido in new[] { "SuperAdmin", Roles.Desenvolvedor, Roles.Motorista })
        {
            var invalido = Autenticado(HttpMethod.Post, "/v1/AuthAdmin/register", tokenDev);
            invalido.Content = JsonContent.Create(NovoUsuario(aurora.EmpresaId, $"user-{papelInvalido}", papelInvalido));
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(invalido)).StatusCode);
        }

        var empresaInexistente = Autenticado(HttpMethod.Post, "/v1/AuthAdmin/register", tokenDev);
        empresaInexistente.Content = JsonContent.Create(NovoUsuario(Guid.NewGuid(), "orfao"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(empresaInexistente)).StatusCode);

        using var db = Fixture.CreateDbContext();
        Assert.False(await db.Roles.AnyAsync(r => r.Name == "SuperAdmin"));
        Assert.False(await db.Users.AnyAsync(u => u.UserName!.StartsWith("user-") || u.UserName == "orfao"));
    }

    [Fact]
    public async Task ListaDePapeisDoFormularioNaoOfereceMotoristaNemDesenvolvedor()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var empresa = await RegistrarEmpresaAsync(client, "Papeis");

        var resposta = await client.SendAsync(Autenticado(HttpMethod.Get, "/v1/usuarios/roles", empresa.Token));
        var papeis = (await resposta.Content.ReadFromJsonAsync<string[]>())!;

        Assert.Equal(Roles.AdminLevel.OrderBy(r => r), papeis);
    }

    [Fact]
    public async Task SyncDesenvolvedores_RemovePapelDeQuemSaiuDaConfig()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        await RegistrarEmpresaAsync(client, "Ex Dev", "ex-dev@truckflow.test");

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var exDev = (await userManager.FindByEmailAsync("ex-dev@truckflow.test"))!;
        await userManager.AddToRoleAsync(exDev, Roles.Desenvolvedor);

        await scope.ServiceProvider.SyncDesenvolvedoresAsync();

        Assert.False(await userManager.IsInRoleAsync(exDev, Roles.Desenvolvedor));
        Assert.True(await userManager.IsInRoleAsync(exDev, Roles.Admin));
    }

    [Fact]
    public async Task EmpresaDesativada_BloqueiaLoginRefreshWebERefreshMobile()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString, EmailDev);
        using var client = factory.CreateApiClient();

        var cliente = await RegistrarEmpresaAsync(client, "Contrato Encerrado");
        var tokenDev = await CriarDesenvolvedorAsync(factory, client);

        var refreshAntes = new HttpRequestMessage(HttpMethod.Post, "/v1/Auth/refresh");
        refreshAntes.Headers.Add("Cookie", $"tf_refresh={cliente.RefreshToken}");
        var respostaAntes = await client.SendAsync(refreshAntes);
        Assert.Equal(HttpStatusCode.OK, respostaAntes.StatusCode);
        var refreshAtual = LerRefreshCookie(respostaAntes);

        var desativar = await client.SendAsync(Autenticado(HttpMethod.Delete, $"/v1/Empresa/{cliente.EmpresaId}", tokenDev));
        Assert.Equal(HttpStatusCode.NoContent, desativar.StatusCode);

        var login = await client.PostAsJsonAsync("/v1/AuthAdmin/login", new { login = cliente.Username, password = Senha });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains("Empresa desativada", await login.Content.ReadAsStringAsync());

        var refreshWeb = new HttpRequestMessage(HttpMethod.Post, "/v1/Auth/refresh");
        refreshWeb.Headers.Add("Cookie", $"tf_refresh={refreshAtual}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(refreshWeb)).StatusCode);

        var outraSessao = await RegistrarSessaoExtraAsync(factory, cliente.UsuarioId, cliente.EmpresaId);
        var refreshMobile = await client.PostAsJsonAsync("/v1/Auth/refresh-mobile", new { refreshToken = outraSessao });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshMobile.StatusCode);
    }

    [Fact]
    public async Task EmpresaDesativada_BloqueiaProvisionamentoSso()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var empresa = TestDataSeeder.NovaEmpresa("SSO Desativada");
        empresa.Ativa = false;
        empresa.Configuracoes = """{"authMethods":["EntraId"]}""";

        using (var db = Fixture.CreateDbContext())
        {
            db.Empresa.Add(empresa);
            db.Set<EntraGroupRoleMapping>().Add(new EntraGroupRoleMapping
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresa.Id,
                EntraGroupId = "grupo-portaria",
                EntraGroupNome = "Portaria",
                RoleName = Roles.Portaria,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var resposta = await client.PostAsJsonAsync("/v1/dev/seed-entra-login", new
        {
            entraObjectId = Guid.NewGuid().ToString(),
            email = "sso@desativada.test",
            nomeExibicao = "Usuario SSO",
            entraGroupIds = new[] { "grupo-portaria" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("EmpresaDesativada", (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("motivo").GetString());

        using var dbCheck = Fixture.CreateDbContext();
        Assert.False(await dbCheck.Users.AnyAsync(u => u.Email == "sso@desativada.test"));
    }

    private sealed record EmpresaRegistrada(Guid EmpresaId, Guid UsuarioId, string Username, string Token, string RefreshToken);

    private static async Task<EmpresaRegistrada> RegistrarEmpresaAsync(HttpClient client, string nome, string? emailAdmin = null)
    {
        var slug = nome.ToLowerInvariant().Replace(' ', '-');
        var username = $"admin-{slug}";

        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"{nome} LTDA",
            nomeFantasia = nome,
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato@{slug}.test",
            nomeAdmin = $"Admin {nome}",
            username,
            emailAdmin = emailAdmin ?? $"{username}@{slug}.test",
            telefone = "41999999999",
            password = Senha,
            logradouro = "Rua Teste",
            numero = "100",
            bairro = "Centro",
            cidade = "Curitiba",
            estado = "PR",
            cep = "80000-000",
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var usuario = corpo.GetProperty("usuario");

        return new EmpresaRegistrada(
            usuario.GetProperty("empresaId").GetGuid(),
            usuario.GetProperty("id").GetGuid(),
            username,
            corpo.GetProperty("token").GetString()!,
            LerRefreshCookie(resposta));
    }

    private static async Task<string> CriarDesenvolvedorAsync(TruckFlowApiFactory factory, HttpClient client)
    {
        await RegistrarEmpresaAsync(client, "Plataforma", EmailDev);

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.SyncDesenvolvedoresAsync();
        }

        var login = await client.PostAsJsonAsync("/v1/AuthAdmin/login", new { login = EmailDev, password = Senha });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static async Task<string> RegistrarSessaoExtraAsync(TruckFlowApiFactory factory, Guid usuarioId, Guid empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var refreshService = scope.ServiceProvider.GetRequiredService<TruckFlow.Application.Interfaces.IRefreshTokenService>();
        var emitido = await refreshService.IssueAsync(usuarioId, empresaId, "app-motorista-teste", null);
        return emitido.RawToken;
    }

    private static string LerRefreshCookie(HttpResponseMessage resposta)
    {
        var cookie = resposta.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("tf_refresh="));
        return cookie["tf_refresh=".Length..cookie.IndexOf(';')];
    }

    private static object NovoUsuario(Guid empresaId, string username, string role = "Admin") => new
    {
        email = $"{username}@teste.com",
        password = Senha,
        username,
        nomeReal = username,
        telefone = "41999999999",
        role,
        empresaId,
    };

    private static HttpRequestMessage Autenticado(HttpMethod metodo, string url, string token)
    {
        var request = new HttpRequestMessage(metodo, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
