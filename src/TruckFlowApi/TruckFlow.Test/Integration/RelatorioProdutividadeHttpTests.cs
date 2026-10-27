using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class RelatorioProdutividadeHttpTests : IntegrationTestBase
{
    public RelatorioProdutividadeHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Produtividade_EsperaDescargaEPatio_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone));
        var dia = hoje.AddDays(-2);

        await SemearAsync(empresaA, dia);
        await SemearAsync(empresaB, dia);
        await SemearAsync(empresaB, dia.AddDays(-1));

        var url = $"/v1/Relatorio/produtividade?inicio={dia:yyyy-MM-dd}&fim={dia:yyyy-MM-dd}&limiteEsperaMin=15";
        var r = await Get(client, url, tokenA);

        var ind = r.GetProperty("indicadores");
        Assert.Equal(2m, ind.GetProperty("ciclos").GetProperty("valor").GetDecimal());
        Assert.Equal(10m, ind.GetProperty("esperaMediaMin").GetProperty("valor").GetDecimal());
        Assert.Equal(20m, ind.GetProperty("esperaP90Min").GetProperty("valor").GetDecimal());
        Assert.Equal(45m, ind.GetProperty("descargaMediaMin").GetProperty("valor").GetDecimal());
        Assert.Equal(1, ind.GetProperty("acimaDoLimite").GetInt32());
        Assert.Equal(0m, ind.GetProperty("ciclos").GetProperty("anterior").GetDecimal());
        Assert.Equal(15, r.GetProperty("limiteEsperaMin").GetInt32());
        Assert.Equal("Doca 1", r.GetProperty("porDoca")[0].GetProperty("nome").GetString());

        var outraDoca = await Get(client, $"{url}&localDescargaId={Guid.NewGuid()}", tokenA);
        Assert.Equal(0m, outraDoca.GetProperty("indicadores").GetProperty("ciclos").GetProperty("valor").GetDecimal());

        var patio = await Get(client, $"/v1/Relatorio/produtividade/patio?dia={dia:yyyy-MM-dd}", tokenA);
        Assert.Equal(3, patio.GetProperty("caminhoes").GetInt32());
        Assert.Equal(JsonValueKind.Null, patio.GetProperty("agoraUtc").ValueKind);
        var docas = patio.GetProperty("docas");
        Assert.Equal(1, docas.GetArrayLength());
        var caminhoes = docas[0].GetProperty("caminhoes");
        Assert.True(caminhoes[1].GetProperty("inicioEstimado").GetBoolean());
        Assert.Equal(20m, caminhoes[1].GetProperty("esperaMin").GetDecimal());
        Assert.Equal("EmAndamento", caminhoes[2].GetProperty("status").GetString());

        var vazio = await Get(client, $"/v1/Relatorio/produtividade/patio?dia={dia.AddDays(-1):yyyy-MM-dd}", tokenA);
        Assert.Equal(0, vazio.GetProperty("caminhoes").GetInt32());

        var invertido = await client.SendAsync(Req($"/v1/Relatorio/produtividade?inicio={hoje:yyyy-MM-dd}&fim={hoje.AddDays(-1):yyyy-MM-dd}", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
    }

    private async Task SemearAsync(Guid empresaId, DateOnly dia)
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

        var oito = RelatorioBiPeriodo.InicioDoDiaUtc(dia).AddHours(8);

        Agendamento Slot(int hora, StatusAgendamento status, string placa)
        {
            var a = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, oito.AddHours(hora - 8), oito.AddHours(hora - 7), status);
            a.PlacaVeiculo = placa;
            return a;
        }

        var primeiro = Slot(8, StatusAgendamento.Finalizado, "PRD1A11");
        primeiro.ChegouEm = oito.AddMinutes(-10);
        primeiro.FimDescargaEm = oito.AddMinutes(40);

        var segundo = Slot(9, StatusAgendamento.Finalizado, "PRD2B22");
        segundo.ChegouEm = oito.AddMinutes(20);
        segundo.FimDescargaEm = oito.AddMinutes(80);

        var noPatio = Slot(10, StatusAgendamento.EmAndamento, "PRD3C33");
        noPatio.ChegouEm = oito.AddMinutes(100);

        db.Agendamento.AddRange(primeiro, segundo, noPatio, Slot(11, StatusAgendamento.Expirado, "PRD4D44"));
        await db.SaveChangesAsync();
    }

    private static async Task<JsonElement> Get(HttpClient client, string url, string token)
    {
        var resposta = await client.SendAsync(Req(url, token));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Produt {slug} {sufixo} LTDA",
            nomeFantasia = $"Produt {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@produt.test",
            nomeAdmin = "Admin Produt",
            username = $"admin-produt-{sufixo}",
            emailAdmin = $"admin-{sufixo}@produt.test",
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
