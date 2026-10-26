using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class CadastroMotoristaHttpTests : IntegrationTestBase
{
    public CadastroMotoristaHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task SenhaFraca_DevolveORequisitoQueFaltaEmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var resposta = await Registrar(client, "motorista-fraco", "41999998888", "abc");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(
            "A senha precisa ter: pelo menos 10 caracteres, uma letra maiúscula, um número, um caractere especial (ex.: @, #, !).",
            await Mensagem(resposta));
    }

    [Fact]
    public async Task UsuarioDuplicado_DevolveMensagemEmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.OK, (await Registrar(client, "motorista-dup", "41999998888")).StatusCode);

        var resposta = await Registrar(client, "motorista-dup", "41999997777");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("Este usuário já está em uso. Escolha outro.", await Mensagem(resposta));
    }

    [Fact]
    public async Task EmailDuplicado_DevolveMensagemEmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.OK, (await Registrar(client, "motorista-email-a", "41999998888", email: "mesmo@teste.com")).StatusCode);

        var resposta = await Registrar(client, "motorista-email-b", "41999997777", email: "mesmo@teste.com");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("Este e-mail já está cadastrado.", await Mensagem(resposta));
    }

    [Fact]
    public async Task TelefoneComMascara_EhSalvoSoComDigitos()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var resposta = await Registrar(client, "motorista-mascara", "(41) 99999-8888");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        await using var db = Fixture.CreateDbContext();
        var usuario = await db.Users.AsNoTracking().Include(u => u.Motorista).SingleAsync(u => u.UserName == "motorista-mascara");
        Assert.Equal("41999998888", usuario.PhoneNumber);
        Assert.Equal("41999998888", usuario.Motorista!.Telefone);
    }

    [Fact]
    public async Task TelefoneSemDdd_DevolveMensagemClara()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var resposta = await Registrar(client, "motorista-sem-ddd", "99999-8888");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Telefone inválido", await Mensagem(resposta));
    }

    [Fact]
    public async Task EditarPerfilComSenhaFraca_DevolveMensagemEmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.OK, (await Registrar(client, "motorista-edita", "41999998888")).StatusCode);
        var login = await client.PostAsJsonAsync("/v1/AuthMotorista/login", new { login = "motorista-edita", password = "SenhaForte@123" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var editar = new HttpRequestMessage(HttpMethod.Put, "/v1/AuthMotorista/me")
        {
            Content = JsonContent.Create(new { password = "curta" }),
        };
        editar.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var resposta = await client.SendAsync(editar);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.StartsWith("A senha precisa ter:", await Mensagem(resposta));
    }

    private static Task<HttpResponseMessage> Registrar(HttpClient client, string username, string telefone, string senha = "SenhaForte@123", string? email = null) =>
        client.PostAsJsonAsync("/v1/AuthMotorista/register", new
        {
            userName = username,
            nomeReal = $"Nome {username}",
            email = email ?? $"{username}@teste.com",
            telefone,
            password = senha,
        });

    private static async Task<string> Mensagem(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;
}
