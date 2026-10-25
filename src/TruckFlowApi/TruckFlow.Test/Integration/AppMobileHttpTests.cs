using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class AppMobileHttpTests : IntegrationTestBase
{
    public AppMobileHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task VersaoMinima_EhPublicaEDevolveConfigDoAppsettings()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var resposta = await client.GetAsync("/v1/app/versao-minima");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("1.0.0", corpo.GetProperty("android").GetString());
        Assert.Equal("1.0.0", corpo.GetProperty("ios").GetString());
    }
}
