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
public class RelatorioFornecedoresHttpTests : IntegrationTestBase
{
    public RelatorioFornecedoresHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Fornecedores_RankingEDetalhe_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone));
        var dia = hoje.AddDays(-2);

        var (pontual, faltoso) = await SemearAsync(empresaA, dia);
        var (deOutraEmpresa, _) = await SemearAsync(empresaB, dia);

        var periodo = $"inicio={dia:yyyy-MM-dd}&fim={dia:yyyy-MM-dd}";
        var r = await Get(client, $"/v1/Relatorio/fornecedores?{periodo}", tokenA);

        var ranking = r.GetProperty("ranking");
        Assert.Equal(2, ranking.GetArrayLength());
        Assert.Equal("Pontual Grãos", ranking[0].GetProperty("nome").GetString());
        Assert.Equal(100m, ranking[0].GetProperty("pontualidadePct").GetDecimal());
        Assert.Equal(100m, ranking[0].GetProperty("score").GetDecimal());

        var ruim = ranking[1];
        Assert.Equal("Faltoso Agro", ruim.GetProperty("nome").GetString());
        Assert.Equal(2, ruim.GetProperty("naoCompareceu").GetInt32());
        Assert.Equal(66.7m, ruim.GetProperty("noShowPct").GetDecimal());
        Assert.Equal(1, ruim.GetProperty("atrasados").GetInt32());
        Assert.Equal(1, ruim.GetProperty("cancelados").GetInt32());

        var ind = r.GetProperty("indicadores");
        Assert.Equal(2m, ind.GetProperty("fornecedoresAtivos").GetProperty("valor").GetDecimal());
        Assert.Equal(75m, ind.GetProperty("pontualidadePct").GetProperty("valor").GetDecimal());

        var soUm = await Get(client, $"/v1/Relatorio/fornecedores?{periodo}&fornecedorId={pontual}", tokenA);
        Assert.Equal(1, soUm.GetProperty("ranking").GetArrayLength());

        var detalhe = await Get(client, $"/v1/Relatorio/fornecedores/detalhe?{periodo}&fornecedorId={faltoso}", tokenA);
        Assert.Equal("Faltoso Agro", detalhe.GetProperty("fornecedor").GetProperty("nome").GetString());
        Assert.Equal(2, detalhe.GetProperty("posicao").GetInt32());
        Assert.Equal(2, detalhe.GetProperty("totalFornecedores").GetInt32());
        Assert.Equal(4, detalhe.GetProperty("ocorrencias").GetArrayLength());
        Assert.Equal("Todos os fornecedores", detalhe.GetProperty("media").GetProperty("nome").GetString());

        var outraEmpresa = await client.SendAsync(Req($"/v1/Relatorio/fornecedores/detalhe?{periodo}&fornecedorId={deOutraEmpresa}", tokenA));
        Assert.Equal(HttpStatusCode.NotFound, outraEmpresa.StatusCode);

        var semFornecedor = await client.SendAsync(Req($"/v1/Relatorio/fornecedores/detalhe?{periodo}", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, semFornecedor.StatusCode);

        var invertido = await client.SendAsync(Req($"/v1/Relatorio/fornecedores?inicio={hoje:yyyy-MM-dd}&fim={hoje.AddDays(-1):yyyy-MM-dd}", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
    }

    private async Task<(Guid Pontual, Guid Faltoso)> SemearAsync(Guid empresaId, DateOnly dia)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
        var pontual = TestDataSeeder.NovoFornecedor(empresaId);
        pontual.Nome = "Pontual Grãos";
        var faltoso = TestDataSeeder.NovoFornecedor(empresaId);
        faltoso.Nome = "Faltoso Agro";
        db.UnidadeEntrega.Add(unidade);
        db.LocalDescarga.Add(doca);
        db.Produto.Add(produto);
        db.Fornecedor.AddRange(pontual, faltoso);
        await db.SaveChangesAsync();

        var seis = RelatorioBiPeriodo.InicioDoDiaUtc(dia).AddHours(6);

        Agendamento Slot(int hora, Fornecedor fornecedor, StatusAgendamento status, int? chegouMin = null)
        {
            var inicio = seis.AddHours(hora - 6);
            var a = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, inicio, inicio.AddMinutes(60), status);
            a.PlacaVeiculo = $"FOR{hora:00}A1";
            a.FornecedorId = fornecedor.Id;
            a.VolumeCarga = 30000m;
            if (chegouMin.HasValue)
            {
                a.ChegouEm = inicio.AddMinutes(chegouMin.Value);
                a.FimDescargaEm = a.ChegouEm.Value.AddMinutes(30);
            }
            return a;
        }

        db.Agendamento.AddRange(
            Slot(6, pontual, StatusAgendamento.Finalizado, -10),
            Slot(7, pontual, StatusAgendamento.Finalizado, 5),
            Slot(8, pontual, StatusAgendamento.Finalizado, 0),
            Slot(9, faltoso, StatusAgendamento.Finalizado, 90),
            Slot(11, faltoso, StatusAgendamento.Expirado),
            Slot(12, faltoso, StatusAgendamento.Expirado),
            Slot(13, faltoso, StatusAgendamento.Cancelado));
        await db.SaveChangesAsync();

        return (pontual.Id, faltoso.Id);
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
            razaoSocial = $"Fornec {slug} {sufixo} LTDA",
            nomeFantasia = $"Fornec {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@fornec.test",
            nomeAdmin = "Admin Fornec",
            username = $"admin-fornec-{sufixo}",
            emailAdmin = $"admin-{sufixo}@fornec.test",
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
