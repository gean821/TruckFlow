using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class DashboardHojeHttpTests : IntegrationTestBase
{
    public DashboardHojeHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Hoje_MostraPatioDocasEChegadas_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        var agora = DateTime.UtcNow;
        await SemearAsync(empresaA,
            (StatusAgendamento.EmAndamento, agora.AddMinutes(-20), agora.AddMinutes(-30), "PAT1O00"),
            (StatusAgendamento.Agendado, agora.AddMinutes(5), null, "PRX1M00"));
        await SemearAsync(empresaB,
            (StatusAgendamento.EmAndamento, agora.AddMinutes(-10), agora.AddMinutes(-15), "OUT1R00"));

        var resposta = await client.SendAsync(Req("/v1/Dashboard/hoje", tokenA));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var hoje = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, hoje.GetProperty("indicadores").GetProperty("noPatio").GetProperty("valor").GetDecimal());
        Assert.Equal("PAT1O00", hoje.GetProperty("patio").GetProperty("maisAntigo").GetProperty("placa").GetString());
        Assert.Equal(1, hoje.GetProperty("docas").GetArrayLength());
        Assert.True(hoje.GetProperty("docas")[0].GetProperty("ocupada").GetBoolean());
        Assert.Contains(hoje.GetProperty("proximasChegadas").EnumerateArray(), c => c.GetProperty("placa").GetString() == "PRX1M00");
        Assert.Equal(24, hoje.GetProperty("porHora").GetArrayLength());
    }

    private async Task SemearAsync(Guid empresaId, params (StatusAgendamento Status, DateTime Inicio, DateTime? Chegou, string Placa)[] itens)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
        db.UnidadeEntrega.Add(unidade);
        db.LocalDescarga.Add(doca);
        db.Produto.Add(produto);
        await db.SaveChangesAsync();

        foreach (var item in itens)
        {
            var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
                empresaId, doca.Id, unidade.Id, produto.Id, item.Inicio, item.Inicio.AddMinutes(15), item.Status);
            agendamento.PlacaVeiculo = item.Placa;
            agendamento.ChegouEm = item.Chegou;
            db.Agendamento.Add(agendamento);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Hoje {slug} {sufixo} LTDA",
            nomeFantasia = $"Hoje {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@hoje.test",
            nomeAdmin = "Admin Hoje",
            username = $"admin-hoje-{sufixo}",
            emailAdmin = $"admin-{sufixo}@hoje.test",
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

    private static HttpRequestMessage Req(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
