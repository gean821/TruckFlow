using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class CicloCaminhaoHttpTests : IntegrationTestBase
{
    public CicloCaminhaoHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task CheckInFinalizarECheckOut_GravamChegadaFimESaidaEmOrdem()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaId, token) = await RegistrarEmpresaAsync(client);
        var agendamentoId = await CriarAgendadoAsync(empresaId);

        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(Req(HttpMethod.Patch, $"/v1/AgendamentoAdmin/{agendamentoId}/check-in", token))).StatusCode);

        var finalizar = Req(HttpMethod.Post, $"/v1/AgendamentoAdmin/{agendamentoId}/finalizar", token);
        finalizar.Content = JsonContent.Create(25000m);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(finalizar)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(Req(HttpMethod.Patch, $"/v1/AgendamentoAdmin/{agendamentoId}/check-out", token))).StatusCode);

        await using var db = Fixture.CreateDbContext();
        var agendamento = await db.Agendamento.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == agendamentoId);

        Assert.Equal(StatusAgendamento.Finalizado, agendamento.StatusAgendamento);
        Assert.NotNull(agendamento.ChegouEm);
        Assert.NotNull(agendamento.FimDescargaEm);
        Assert.NotNull(agendamento.SaiuEm);
        Assert.True(agendamento.ChegouEm <= agendamento.FimDescargaEm);
        Assert.True(agendamento.FimDescargaEm <= agendamento.SaiuEm);
        Assert.Null(agendamento.ChamadoDocaEm);
        Assert.Null(agendamento.InicioDescargaEm);

        var detalhe = await client.SendAsync(Req(HttpMethod.Get, $"/v1/AgendamentoAdmin/{agendamentoId}", token));
        var corpo = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.String, corpo.GetProperty("chegouEm").ValueKind);
        Assert.Equal(JsonValueKind.String, corpo.GetProperty("fimDescargaEm").ValueKind);
        Assert.Equal(JsonValueKind.String, corpo.GetProperty("saiuEm").ValueKind);
    }

    [Fact]
    public async Task CheckOutDiretoDoPatio_FinalizaEGravaFimESaida()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaId, token) = await RegistrarEmpresaAsync(client);
        var agendamentoId = await CriarAgendadoAsync(empresaId);

        await client.SendAsync(Req(HttpMethod.Patch, $"/v1/AgendamentoAdmin/{agendamentoId}/check-in", token));
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(Req(HttpMethod.Patch, $"/v1/AgendamentoAdmin/{agendamentoId}/check-out", token))).StatusCode);

        await using var db = Fixture.CreateDbContext();
        var agendamento = await db.Agendamento.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == agendamentoId);
        Assert.Equal(StatusAgendamento.Finalizado, agendamento.StatusAgendamento);
        Assert.NotNull(agendamento.FimDescargaEm);
        Assert.NotNull(agendamento.SaiuEm);
    }

    private async Task<Guid> CriarAgendadoAsync(Guid empresaId)
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

        var inicio = DateTime.UtcNow.AddMinutes(30);
        var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, inicio, inicio.AddMinutes(30));
        agendamento.PlacaVeiculo = "ABC1D23";
        db.Agendamento.Add(agendamento);
        await db.SaveChangesAsync();

        return agendamento.Id;
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Ciclo {sufixo} LTDA",
            nomeFantasia = $"Ciclo {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@ciclo.test",
            nomeAdmin = "Admin Ciclo",
            username = $"admin-ciclo-{sufixo}",
            emailAdmin = $"admin-{sufixo}@ciclo.test",
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

    private static HttpRequestMessage Req(HttpMethod metodo, string url, string token)
    {
        var request = new HttpRequestMessage(metodo, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
