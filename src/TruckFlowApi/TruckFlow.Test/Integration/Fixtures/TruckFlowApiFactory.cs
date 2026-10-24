using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace TruckFlow.Test.Integration.Fixtures;

internal sealed class TruckFlowApiFactory : WebApplicationFactory<TruckFlow.Program>
{
    private readonly string _connectionString;
    private readonly string[] _desenvolvedorEmails;

    public TruckFlowApiFactory(string connectionString, params string[] desenvolvedorEmails)
    {
        _connectionString = connectionString;
        _desenvolvedorEmails = desenvolvedorEmails;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("JwtOptions:SecurityKey", new string('k', 64) + "-truckflow-testes-integracao-http");
        builder.UseSetting("JwtOptions:Issuer", "TruckFlow");
        builder.UseSetting("JwtOptions:Audience", "TruckFlow");
        builder.UseSetting("EntraIdOptions:ClientId", "");

        for (var i = 0; i < _desenvolvedorEmails.Length; i++)
        {
            builder.UseSetting($"Plataforma:DesenvolvedorEmails:{i}", _desenvolvedorEmails[i]);
        }

        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
    }

    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
}
