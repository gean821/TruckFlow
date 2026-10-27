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
public class RelatorioMotoristaHttpTests : IntegrationTestBase
{
    private const string Senha = "SenhaForte@123";

    public RelatorioMotoristaHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Relatorio_SomaSoOMotoristaLogado_EmTodasAsFabricas_ComDiaDeBrasilia()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (tokenA, usuarioA) = await RegistrarMotoristaAsync(client, "rel-a");
        var (_, usuarioB) = await RegistrarMotoristaAsync(client, "rel-b");

        var fabricaX = await CriarFabricaAsync("Fabrica X", "Unidade Norte", "Soja");
        var fabricaY = await CriarFabricaAsync("Fabrica Y", "Unidade Sul", "Milho");

        var proximo = Utc(DateTime.UtcNow.Date.AddDays(3).AddHours(15));

        await SemearAsync(fabricaX,
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 9, 10, 12), 30000m),
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 9, 20, 12), 20000m),
            (usuarioA, StatusAgendamento.Expirado, Utc(2026, 9, 5, 12), 0m),
            (usuarioA, StatusAgendamento.Cancelado, Utc(2026, 9, 6, 12), 0m),
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 8, 15, 12), 10000m),
            (usuarioB, StatusAgendamento.Finalizado, Utc(2026, 9, 12, 15), 99000m),
            (usuarioA, StatusAgendamento.Agendado, proximo, 0m));

        await SemearAsync(fabricaY,
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 9, 15, 12), 10000m),
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 10, 1, 2), 5000m),
            (usuarioA, StatusAgendamento.Finalizado, Utc(2026, 9, 1, 2), 40000m));

        var relatorio = await GetRelatorio(client, tokenA, "inicio=2026-09-01&fim=2026-09-30");

        var atual = relatorio.GetProperty("atual");
        Assert.Equal(6, atual.GetProperty("agendamentos").GetInt32());
        Assert.Equal(4, atual.GetProperty("concluidas").GetInt32());
        Assert.Equal(1, atual.GetProperty("naoCompareceu").GetInt32());
        Assert.Equal(1, atual.GetProperty("canceladas").GetInt32());
        Assert.Equal(65000m, atual.GetProperty("pesoKg").GetDecimal());
        Assert.Equal(80.0m, atual.GetProperty("comparecimentoPct").GetDecimal());

        Assert.Equal("2026-08-02", relatorio.GetProperty("inicioAnterior").GetString());
        var anterior = relatorio.GetProperty("anterior");
        Assert.Equal(2, anterior.GetProperty("concluidas").GetInt32());
        Assert.Equal(50000m, anterior.GetProperty("pesoKg").GetDecimal());
        Assert.Equal(100.0m, relatorio.GetProperty("variacaoEntregasPct").GetDecimal());
        Assert.Equal(30.0m, relatorio.GetProperty("variacaoPesoPct").GetDecimal());

        var produtos = relatorio.GetProperty("porProduto").EnumerateArray().ToList();
        Assert.Equal("Soja", produtos[0].GetProperty("nome").GetString());
        Assert.Equal(76.9m, produtos[0].GetProperty("percentual").GetDecimal());
        Assert.Equal("Milho", produtos[1].GetProperty("nome").GetString());

        var locais = relatorio.GetProperty("porLocal").EnumerateArray().ToList();
        Assert.Equal(2, locais.Count);
        Assert.Equal("Unidade Norte", locais[0].GetProperty("nome").GetString());
        Assert.Equal("Fabrica X", locais[0].GetProperty("detalhe").GetString());

        var historico = relatorio.GetProperty("historicoMensal").EnumerateArray().ToList();
        Assert.Equal(6, historico.Count);
        Assert.Equal(["abr", "mai", "jun", "jul", "ago", "set"], historico.Select(h => h.GetProperty("rotulo").GetString()));
        Assert.Equal(4, historico[5].GetProperty("entregas").GetInt32());
        Assert.Equal(2, historico[4].GetProperty("entregas").GetInt32());

        var proximoDto = relatorio.GetProperty("proximoAgendamento");
        Assert.Equal("Unidade Norte", proximoDto.GetProperty("unidadeEntrega").GetString());
        Assert.Equal("Fabrica X", proximoDto.GetProperty("empresa").GetString());
    }

    [Fact]
    public async Task Relatorio_PeriodoInvalido_Devolve400EmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();
        var (token, _) = await RegistrarMotoristaAsync(client, "rel-invalido");

        var invertido = await client.SendAsync(Autenticado($"/v1/AgendamentoMotorista/relatorio?inicio=2026-09-30&fim=2026-09-01", token));
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
        Assert.Equal("A data final precisa ser igual ou posterior à data inicial.",
            (await invertido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        var longo = await client.SendAsync(Autenticado($"/v1/AgendamentoMotorista/relatorio?inicio=2024-01-01&fim=2026-01-01", token));
        Assert.Equal(HttpStatusCode.BadRequest, longo.StatusCode);
    }

    [Fact]
    public async Task Relatorio_SemAgendamentos_DevolveZerosSemQuebrar()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();
        var (token, _) = await RegistrarMotoristaAsync(client, "rel-vazio");

        var relatorio = await GetRelatorio(client, token, "");

        Assert.Equal(0, relatorio.GetProperty("atual").GetProperty("concluidas").GetInt32());
        Assert.Equal(JsonValueKind.Null, relatorio.GetProperty("atual").GetProperty("comparecimentoPct").ValueKind);
        Assert.Equal(JsonValueKind.Null, relatorio.GetProperty("variacaoEntregasPct").ValueKind);
        Assert.Equal(JsonValueKind.Null, relatorio.GetProperty("proximoAgendamento").ValueKind);
        Assert.Equal(6, relatorio.GetProperty("historicoMensal").GetArrayLength());
    }

    private sealed record Fabrica(Guid EmpresaId, Guid UnidadeId, Guid DocaId, Guid ProdutoId);

    private async Task<Fabrica> CriarFabricaAsync(string nome, string unidadeNome, string produtoNome)
    {
        var empresa = TestDataSeeder.NovaEmpresa(nome);
        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresa.Id);
        unidade.Nome = unidadeNome;
        var doca = TestDataSeeder.NovoLocalDescarga(empresa.Id, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresa.Id, doca.Id, doca);
        produto.Nome = produtoNome;

        await using (var db = Fixture.CreateDbContext())
        {
            db.Empresa.Add(empresa);
            await db.SaveChangesAsync();
        }

        var contexto = ServiceFactory.NewEmpresaContext();
        using (contexto.WithTenant(empresa.Id))
        await using (var db = Fixture.CreateDbContext(contexto))
        {
            db.UnidadeEntrega.Add(unidade);
            db.LocalDescarga.Add(doca);
            db.Produto.Add(produto);
            await db.SaveChangesAsync();
        }

        return new Fabrica(empresa.Id, unidade.Id, doca.Id, produto.Id);
    }

    private async Task SemearAsync(Fabrica fabrica, params (Guid UsuarioId, StatusAgendamento Status, DateTime Inicio, decimal PesoKg)[] itens)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(fabrica.EmpresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        foreach (var item in itens)
        {
            var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
                fabrica.EmpresaId, fabrica.DocaId, fabrica.UnidadeId, fabrica.ProdutoId,
                item.Inicio, item.Inicio.AddMinutes(30), item.Status);
            agendamento.UsuarioId = item.UsuarioId;
            agendamento.PlacaVeiculo = "ABC1D23";
            agendamento.VolumeCarga = item.PesoKg;
            db.Agendamento.Add(agendamento);
        }

        await db.SaveChangesAsync();
    }

    private async Task<(string Token, Guid UsuarioId)> RegistrarMotoristaAsync(HttpClient client, string prefixo)
    {
        var username = $"{prefixo}-{Guid.NewGuid():N}"[..20];
        var registro = await client.PostAsJsonAsync("/v1/AuthMotorista/register", new
        {
            userName = username,
            nomeReal = $"Motorista {prefixo}",
            email = $"{username}@teste.com",
            telefone = "41999998888",
            password = Senha,
        });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);

        var login = await client.PostAsJsonAsync("/v1/AuthMotorista/login", new { login = username, password = Senha });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        await using var db = Fixture.CreateDbContext();
        var usuarioId = await db.Users.Where(u => u.UserName == username).Select(u => u.Id).SingleAsync();

        return (token, usuarioId);
    }

    private static async Task<JsonElement> GetRelatorio(HttpClient client, string token, string query)
    {
        var resposta = await client.SendAsync(Autenticado($"/v1/AgendamentoMotorista/relatorio?{query}", token));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static HttpRequestMessage Autenticado(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static DateTime Utc(int ano, int mes, int dia, int hora) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private static DateTime Utc(DateTime data) => DateTime.SpecifyKind(data, DateTimeKind.Utc);
}
