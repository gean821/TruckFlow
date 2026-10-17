using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using TruckFlow.Extensions.RateLimiting;

namespace TruckFlow.Test.Integration;

public class RateLimitingTests
{
    private const string LoginAdmin = "/v1/AuthAdmin/login";
    private const string LoginMotorista = "/v1/AuthMotorista/login";
    private const string RotaQualquer = "/v1/Agendamento";

    private static async Task<(WebApplication App, HttpClient Client)> CriarHostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRateLimitingDependency();

        var app = builder.Build();

        app.Use(
            (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Test-Ip", out var ip))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip!);
                }

                return next();
            }
        );

        app.UseRateLimitingDependency();

        app.MapPost(LoginAdmin, () => Results.Ok());
        app.MapPost(LoginMotorista, () => Results.Ok());
        app.MapPost(RotaQualquer, () => Results.Ok());

        await app.StartAsync();
        return (app, app.GetTestServer().CreateClient());
    }

    private static HttpRequestMessage LoginRequest(string path, string ip, string login) =>
        new(HttpMethod.Post, path)
        {
            Headers = { { "X-Test-Ip", ip } },
            Content = JsonContent(login),
        };

    private static StringContent JsonContent(string login) =>
        new(
            $$"""{"login":"{{login}}","password":"qualquer"}""",
            Encoding.UTF8,
            "application/json"
        );

    [Fact]
    public async Task SextaTentativaDoMesmoIp_RetornaTooManyRequests_MesmoComContasDiferentes()
    {
        var (app, client) = await CriarHostAsync();
        using var _ = app;

        const string ip = "10.10.0.1";
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 5; i++)
        {
            ultima = await client.SendAsync(LoginRequest(LoginAdmin, ip, $"conta{i}@teste.com"));
            Assert.Equal(HttpStatusCode.OK, ultima.StatusCode);
        }

        ultima = await client.SendAsync(
            LoginRequest(LoginAdmin, ip, "conta-nova-nunca-usada@teste.com")
        );

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);
        Assert.True(ultima.Headers.Contains("Retry-After"));

        var corpo = await ultima.Content.ReadAsStringAsync();
        Assert.Contains("RATE_LIMITED", corpo);
    }

    [Fact]
    public async Task SextaTentativaNaMesmaConta_RetornaTooManyRequests_MesmoTrocandoDeIp()
    {
        var (app, client) = await CriarHostAsync();
        using var _ = app;

        const string conta = "vitima@teste.com";
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 5; i++)
        {
            ultima = await client.SendAsync(LoginRequest(LoginAdmin, $"10.20.0.{i + 1}", conta));
            Assert.Equal(HttpStatusCode.OK, ultima.StatusCode);
        }

        ultima = await client.SendAsync(LoginRequest(LoginAdmin, "10.20.0.99", conta));

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);
    }

    [Fact]
    public async Task LimitePorIp_EhCompartilhadoEntreAuthAdminEAuthMotorista()
    {
        // A camada por IP não distingue endpoint de propósito: um IP martelando login não
        // ganha orçamento dobrado só por alternar entre AuthAdmin e AuthMotorista.
        var (app, client) = await CriarHostAsync();
        using var _ = app;

        const string ip = "10.30.0.1";
        var caminhos = new[] { LoginAdmin, LoginMotorista, LoginAdmin, LoginMotorista, LoginAdmin };
        for (var i = 0; i < caminhos.Length; i++)
        {
            var r = await client.SendAsync(LoginRequest(caminhos[i], ip, $"conta{i}@teste.com"));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var bloqueado = await client.SendAsync(LoginRequest(LoginMotorista, ip, "outra@teste.com"));
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueado.StatusCode);
    }

    [Fact]
    public async Task RotaQueNaoEhDeLogin_NuncaELimitada()
    {
        var (app, client) = await CriarHostAsync();
        using var _ = app;

        const string ip = "10.40.0.1";
        for (var i = 0; i < 20; i++)
        {
            var r = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, RotaQualquer)
                {
                    Headers = { { "X-Test-Ip", ip } },
                }
            );
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }
    }

    [Fact]
    public async Task AbaixoDoLimite_LoginContinuaFuncionandoNormalmente()
    {
        var (app, client) = await CriarHostAsync();
        using var _ = app;

        var resposta = await client.SendAsync(
            LoginRequest(LoginAdmin, "10.50.0.1", "normal@teste.com")
        );

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
