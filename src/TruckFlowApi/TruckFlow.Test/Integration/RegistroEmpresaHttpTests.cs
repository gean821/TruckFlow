using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class RegistroEmpresaHttpTests : IntegrationTestBase
{
    public RegistroEmpresaHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task SenhaFracaDoAdmin_Devolve400EmPortuguesSemCriarEmpresa()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = "Senha Fraca LTDA",
            nomeFantasia = "Senha Fraca",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = "contato@fraca.test",
            nomeAdmin = "Admin Fraca",
            username = "admin-fraca",
            emailAdmin = "admin@fraca.test",
            telefone = "41999999999",
            password = "123",
            logradouro = "Rua Teste",
            numero = "100",
            bairro = "Centro",
            cidade = "Curitiba",
            estado = "PR",
            cep = "80000-000",
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var mensagem = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString();
        Assert.Equal(
            "A senha precisa ter: pelo menos 10 caracteres, uma letra maiúscula, uma letra minúscula, um caractere especial (ex.: @, #, !).",
            mensagem);

        await using var db = Fixture.CreateDbContext();
        Assert.False(db.Empresa.Any(e => e.NomeFantasia == "Senha Fraca"));
    }
}
