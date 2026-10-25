using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class ExclusaoContaMotoristaHttpTests : IntegrationTestBase
{
    private const string Senha = "SenhaForte@123";

    public ExclusaoContaMotoristaHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task ExcluirConta_AnonimizaDadosRevogaSessoesERemoveVeiculos()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var alvo = await RegistrarELogarAsync(client, "motorista-alvo", "41911112222");
        var outro = await RegistrarELogarAsync(client, "motorista-outro", "41933334444");

        await using (var db = Fixture.CreateDbContext())
        {
            var motorista = await db.Motorista.SingleAsync(m => m.UsuarioId == alvo.UsuarioId);
            db.Veiculo.Add(new Veiculo { Placa = "ABC1D23", TipoVeiculo = TipoVeiculo.CarretaDoisEixos, Motorista = motorista, MotoristaId = motorista.Id, CreatedAt = DateTime.UtcNow });
            db.DispositivoUsuario.Add(new DispositivoUsuario { UsuarioId = alvo.UsuarioId, ExpoPushToken = "ExponentPushToken[alvo]", Plataforma = PlataformaDispositivo.Android, UltimoUsoEm = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var excluir = new HttpRequestMessage(HttpMethod.Delete, "/v1/AuthMotorista/me");
        excluir.Headers.Authorization = new AuthenticationHeaderValue("Bearer", alvo.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(excluir)).StatusCode);

        var refresh = await client.PostAsJsonAsync("/v1/Auth/refresh-mobile", new { refreshToken = alvo.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var login = await client.PostAsJsonAsync("/v1/AuthMotorista/login", new { login = "motorista-alvo", password = Senha });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var refreshOutro = await client.PostAsJsonAsync("/v1/Auth/refresh-mobile", new { refreshToken = outro.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshOutro.StatusCode);

        await using var dbCheck = Fixture.CreateDbContext();
        var usuario = await dbCheck.Users.AsNoTracking().Include(u => u.Motorista).SingleAsync(u => u.Id == alvo.UsuarioId);

        Assert.NotNull(usuario.DeletedAt);
        Assert.DoesNotContain("motorista-alvo", usuario.UserName);
        Assert.DoesNotContain("motorista-alvo", usuario.Email);
        Assert.Null(usuario.PhoneNumber);
        Assert.Null(usuario.PasswordHash);
        Assert.Equal("Motorista excluído", usuario.Motorista!.NomeReal);
        Assert.Equal(string.Empty, usuario.Motorista.Telefone);
        Assert.False(await dbCheck.Veiculo.AnyAsync(v => v.MotoristaId == usuario.Motorista.Id));
        Assert.False(await dbCheck.DispositivoUsuario.AnyAsync(d => d.UsuarioId == alvo.UsuarioId && d.Ativo));
        Assert.False(await dbCheck.Set<RefreshToken>().AnyAsync(r => r.UserId == alvo.UsuarioId && r.RevokedAt == null));

        var outroUsuario = await dbCheck.Users.AsNoTracking().Include(u => u.Motorista).SingleAsync(u => u.Id == outro.UsuarioId);
        Assert.Equal("motorista-outro", outroUsuario.UserName);
        Assert.Equal("41933334444", outroUsuario.PhoneNumber);
    }

    private sealed record MotoristaLogado(Guid UsuarioId, string Token, string RefreshToken);

    private static async Task<MotoristaLogado> RegistrarELogarAsync(HttpClient client, string username, string telefone)
    {
        var registro = await client.PostAsJsonAsync("/v1/AuthMotorista/register", new
        {
            userName = username,
            nomeReal = $"Nome {username}",
            email = $"{username}@teste.com",
            telefone,
            password = Senha,
        });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);

        var login = await client.PostAsJsonAsync("/v1/AuthMotorista/login", new { login = username, password = Senha });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new MotoristaLogado(
            corpo.GetProperty("usuario").GetProperty("id").GetGuid(),
            corpo.GetProperty("token").GetString()!,
            corpo.GetProperty("refreshToken").GetString()!);
    }
}
