using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class RelatorioVisaoGeralHttpTests : IntegrationTestBase
{
    public RelatorioVisaoGeralHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task VisaoGeral_SoEmpresaLogada_SemVagaVazia_ComDiaDeBrasiliaEFiltro()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        await using (var db = Fixture.CreateDbContext())
        {
            var empresa = await db.Empresa.SingleAsync(e => e.Id == empresaA);
            empresa.Configuracoes = """{"metaDiariaKg":25000}""";
            await db.SaveChangesAsync();
        }

        var fabricaA = await CriarFabricaAsync(empresaA);
        var fabricaB = await CriarFabricaAsync(empresaB);

        await SemearAsync(fabricaA,
            (StatusAgendamento.Finalizado, Utc(2026, 9, 10, 12), 30000m, true, fabricaA.FornecedorId),
            (StatusAgendamento.Finalizado, Utc(2026, 9, 11, 12), 20000m, true, null),
            (StatusAgendamento.Expirado, Utc(2026, 9, 12, 12), 0m, true, null),
            (StatusAgendamento.Expirado, Utc(2026, 9, 13, 12), 0m, false, null),
            (StatusAgendamento.Disponivel, Utc(2026, 9, 14, 12), 0m, false, null),
            (StatusAgendamento.Finalizado, Utc(2026, 10, 1, 2), 5000m, true, null),
            (StatusAgendamento.Finalizado, Utc(2026, 8, 20, 12), 10000m, true, null));

        await SemearAsync(fabricaB,
            (StatusAgendamento.Finalizado, Utc(2026, 9, 10, 15), 99000m, true, null));

        var visao = await Get(client, tokenA, "inicio=2026-09-01&fim=2026-09-30");

        var ind = visao.GetProperty("indicadores");
        Assert.Equal(4, ind.GetProperty("agendamentos").GetProperty("valor").GetDecimal());
        Assert.Equal(3, ind.GetProperty("concluidos").GetProperty("valor").GetDecimal());
        Assert.Equal(55000m, ind.GetProperty("pesoRecebidoKg").GetProperty("valor").GetDecimal());
        Assert.Equal(1, ind.GetProperty("naoCompareceu").GetProperty("valor").GetDecimal());
        Assert.Equal(10000m, ind.GetProperty("pesoRecebidoKg").GetProperty("anterior").GetDecimal());
        Assert.Equal(30, visao.GetProperty("serieDiaria").GetArrayLength());
        Assert.Equal(1, visao.GetProperty("docasAtivas").GetInt32());
        Assert.Equal(7, visao.GetProperty("heatmapDiaHora").GetArrayLength());
        Assert.Equal(24, visao.GetProperty("ocupacaoPorHora").GetArrayLength());

        Assert.Equal(25000m, visao.GetProperty("metaDiariaKg").GetDecimal());

        var filtrado = await Get(client, tokenA, $"inicio=2026-09-01&fim=2026-09-30&fornecedorId={fabricaA.FornecedorId}");
        Assert.Equal(30000m, filtrado.GetProperty("indicadores").GetProperty("pesoRecebidoKg").GetProperty("valor").GetDecimal());
        Assert.Equal(JsonValueKind.Null, filtrado.GetProperty("metaDiariaKg").ValueKind);
        Assert.Equal(JsonValueKind.Null, filtrado.GetProperty("indicadores").GetProperty("atingimentoMetaPct").ValueKind);
    }

    [Fact]
    public async Task VisaoGeral_PeriodoInvertido_Devolve400EmPortugues()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();
        var (_, token) = await RegistrarEmpresaAsync(client, "inv");

        var resposta = await client.SendAsync(Req("/v1/Relatorio/visao-geral?inicio=2026-09-30&fim=2026-09-01", token));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("A data final precisa ser igual ou posterior à data inicial.",
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    private sealed record Fabrica(Guid EmpresaId, Guid UnidadeId, Guid DocaId, Guid ProdutoId, Guid FornecedorId);

    private async Task<Fabrica> CriarFabricaAsync(Guid empresaId)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
        var fornecedor = TestDataSeeder.NovoFornecedor(empresaId);
        db.UnidadeEntrega.Add(unidade);
        db.LocalDescarga.Add(doca);
        db.Produto.Add(produto);
        db.Fornecedor.Add(fornecedor);
        await db.SaveChangesAsync();

        return new Fabrica(empresaId, unidade.Id, doca.Id, produto.Id, fornecedor.Id);
    }

    private async Task SemearAsync(Fabrica fabrica, params (StatusAgendamento Status, DateTime Inicio, decimal Peso, bool Reservado, Guid? FornecedorId)[] itens)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(fabrica.EmpresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        foreach (var item in itens)
        {
            var agendamento = TestDataSeeder.NovoAgendamentoAtivoNaDoca(
                fabrica.EmpresaId, fabrica.DocaId, fabrica.UnidadeId, fabrica.ProdutoId, item.Inicio, item.Inicio.AddMinutes(30), item.Status);
            agendamento.PlacaVeiculo = item.Reservado ? "ABC1D23" : null;
            agendamento.VolumeCarga = item.Peso;
            agendamento.FornecedorId = item.FornecedorId;
            db.Agendamento.Add(agendamento);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<JsonElement> Get(HttpClient client, string token, string query)
    {
        var resposta = await client.SendAsync(Req($"/v1/Relatorio/visao-geral?{query}", token));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Visao {slug} {sufixo} LTDA",
            nomeFantasia = $"Visao {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@visao.test",
            nomeAdmin = "Admin Visao",
            username = $"admin-visao-{sufixo}",
            emailAdmin = $"admin-{sufixo}@visao.test",
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

    private static DateTime Utc(int ano, int mes, int dia, int hora) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);
}
