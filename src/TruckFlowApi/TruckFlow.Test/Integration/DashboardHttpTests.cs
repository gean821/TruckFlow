using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class DashboardHttpTests : IntegrationTestBase
{
    public DashboardHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task MetaDiaria_EditadaPelaEmpresa_ApareceNoDashboardSemPerderAuthMethods()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaId, token) = await RegistrarEmpresaAsync(client, "meta");

        using (var db = Fixture.CreateDbContext())
        {
            var empresa = await db.Empresa.SingleAsync(e => e.Id == empresaId);
            empresa.Configuracoes = """{"authMethods":["Local","EntraId"]}""";
            await db.SaveChangesAsync();
        }

        var salvar = await client.SendAsync(Patch(empresaId, token, new { metaDiariaKg = 480000 }));
        Assert.Equal(HttpStatusCode.OK, salvar.StatusCode);
        Assert.Equal(480000m, (await salvar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("metaDiariaKg").GetDecimal());

        var resumo = await client.SendAsync(Autenticado(HttpMethod.Get, "/v1/Dashboard/summary", token));
        Assert.Equal(HttpStatusCode.OK, resumo.StatusCode);
        var volume = (await resumo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("volume");
        Assert.Equal(480000m, volume.GetProperty("metaDiariaKg").GetDecimal());

        using (var db = Fixture.CreateDbContext())
        {
            var configuracoes = (await db.Empresa.AsNoTracking().SingleAsync(e => e.Id == empresaId)).Configuracoes!;
            Assert.Contains("EntraId", configuracoes);
        }

        var limpar = await client.SendAsync(Patch(empresaId, token, new { metaDiariaKg = 0 }));
        Assert.Equal(HttpStatusCode.OK, limpar.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await limpar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("metaDiariaKg").ValueKind);

        var negativa = await client.SendAsync(Patch(empresaId, token, new { metaDiariaKg = -1 }));
        Assert.Equal(HttpStatusCode.BadRequest, negativa.StatusCode);
    }

    [Fact]
    public async Task ListagemDoDia_ComOcultarVagasLivres_NaoTrazVagaVazia()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaId, token) = await RegistrarEmpresaAsync(client, "agenda");

        var empresaContext = ServiceFactory.NewEmpresaContext();
        var amanha = DateTime.UtcNow.Date.AddDays(1).AddHours(15);

        using (empresaContext.WithTenant(empresaId))
        using (var db = Fixture.CreateDbContext(empresaContext))
        {
            var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
            var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
            var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
            db.UnidadeEntrega.Add(unidade);
            db.LocalDescarga.Add(doca);
            db.Produto.Add(produto);
            await db.SaveChangesAsync();

            db.Agendamento.AddRange(
                TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, amanha, amanha.AddHours(1)),
                TestDataSeeder.NovaVagaDisponivel(empresaId, unidade.Id, produto.Id, amanha.AddHours(2)));
            await db.SaveChangesAsync();
        }

        var dia = amanha.ToString("yyyy-MM-dd");

        var todas = await Listar(client, token, $"dataInicio={dia}&dataFim={dia}");
        Assert.Equal(2, todas.GetProperty("totalCount").GetInt32());

        var semVagas = await Listar(client, token, $"dataInicio={dia}&dataFim={dia}&ocultarVagasLivres=true");
        Assert.Equal(1, semVagas.GetProperty("totalCount").GetInt32());
        Assert.Equal(nameof(StatusAgendamento.Agendado), semVagas.GetProperty("items")[0].GetProperty("status").GetString());
    }

    private static async Task<JsonElement> Listar(HttpClient client, string token, string query)
    {
        var resposta = await client.SendAsync(Autenticado(HttpMethod.Get, $"/v1/AgendamentoAdmin?{query}", token));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Dashboard {slug} LTDA",
            nomeFantasia = $"Dashboard {slug}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@{slug}.test",
            nomeAdmin = $"Admin {slug}",
            username = $"admin-{slug}-{sufixo}",
            emailAdmin = $"admin-{sufixo}@{slug}.test",
            telefone = "41999999999",
            password = "SenhaForte@123",
            logradouro = "Rua Teste",
            numero = "100",
            bairro = "Centro",
            cidade = "Curitiba",
            estado = "PR",
            cep = "80000-000",
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return (corpo.GetProperty("usuario").GetProperty("empresaId").GetGuid(), corpo.GetProperty("token").GetString()!);
    }

    private static HttpRequestMessage Patch(Guid empresaId, string token, object corpo)
    {
        var request = Autenticado(HttpMethod.Patch, $"/v1/Empresa/{empresaId}", token);
        request.Content = JsonContent.Create(corpo);
        return request;
    }

    private static HttpRequestMessage Autenticado(HttpMethod metodo, string url, string token)
    {
        var request = new HttpRequestMessage(metodo, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
