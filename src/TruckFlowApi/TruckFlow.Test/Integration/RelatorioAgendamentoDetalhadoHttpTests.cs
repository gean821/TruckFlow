using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class RelatorioAgendamentoDetalhadoHttpTests : IntegrationTestBase
{
    public RelatorioAgendamentoDetalhadoHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Detalhado_FiltraOrdenaPaginaECalculaCiclo_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone));
        var dia = hoje.AddDays(-2);

        var docaA = await SemearAsync(empresaA, dia);
        await SemearAsync(empresaB, dia);

        var periodo = $"dataInicio={dia:yyyy-MM-dd}&dataFim={dia:yyyy-MM-dd}";
        var url = $"/v1/Relatorio/agendamentos/detalhado?{periodo}";

        var r = await Get(client, url, tokenA);
        Assert.Equal(5, r.GetProperty("totalCount").GetInt32());
        var itens = r.GetProperty("items");
        Assert.Equal("DET1A11", itens[0].GetProperty("placaVeiculo").GetString());

        var segundo = itens[1];
        Assert.True(segundo.GetProperty("inicioEstimado").GetBoolean());
        Assert.Equal(20d, segundo.GetProperty("esperaMin").GetDouble());
        Assert.Equal(40d, segundo.GetProperty("descargaMin").GetDouble());
        Assert.Equal("Adiantado", segundo.GetProperty("situacaoChegada").GetString());
        Assert.Equal("Concluído", segundo.GetProperty("statusRotulo").GetString());

        var pelaGrade = itens[3];
        Assert.Equal("Doca 1", pelaGrade.GetProperty("localDescarga").GetString());
        Assert.Equal("Atrasado", pelaGrade.GetProperty("situacaoChegada").GetString());
        Assert.Equal(70d, pelaGrade.GetProperty("atrasoChegadaMin").GetDouble());

        Assert.Equal("Avulso", itens[4].GetProperty("origem").GetString());
        Assert.Equal("Cooperativa Sul", itens[4].GetProperty("fornecedorNome").GetString());

        var desc = await Get(client, $"{url}&desc=true", tokenA);
        Assert.Equal("DET5E55", desc.GetProperty("items")[0].GetProperty("placaVeiculo").GetString());

        Assert.Equal(2, await Total(client, $"{url}&status=Expirado&status=EmAndamento", tokenA));
        Assert.Equal(5, await Total(client, $"{url}&localDescargaId={docaA}", tokenA));
        Assert.Equal(0, await Total(client, $"{url}&localDescargaId={Guid.NewGuid()}", tokenA));
        Assert.Equal(1, await Total(client, $"{url}&origem=Avulso", tokenA));
        Assert.Equal(1, await Total(client, $"{url}&chegada=Atrasado", tokenA));
        Assert.Equal(1, await Total(client, $"{url}&chegada=NaoCompareceu", tokenA));
        Assert.Equal(2, await Total(client, $"{url}&chegada=Adiantado", tokenA));
        Assert.Equal(1, await Total(client, $"{url}&busca=det5e", tokenA));
        Assert.Equal(1, await Total(client, $"{url}&busca=cooperativa", tokenA));

        var porPlaca = await Get(client, $"{url}&ordenarPor=placa&desc=true", tokenA);
        Assert.Equal("DET5E55", porPlaca.GetProperty("items")[0].GetProperty("placaVeiculo").GetString());

        var pagina = await Get(client, $"{url}&pageSize=2&pageNumber=3", tokenA);
        Assert.Equal(1, pagina.GetProperty("items").GetArrayLength());
        Assert.Equal(3, pagina.GetProperty("totalPages").GetInt32());

        var resumo = await Get(client, $"/v1/Relatorio/agendamentos/resumo?{periodo}", tokenA);
        Assert.Equal(5, resumo.GetProperty("total").GetInt32());
        Assert.Equal(3, resumo.GetProperty("chegaram").GetInt32());
        Assert.Equal(2, resumo.GetProperty("adiantados").GetInt32());
        Assert.Equal(1, resumo.GetProperty("atrasados").GetInt32());
        Assert.Equal(1, resumo.GetProperty("naoCompareceram").GetInt32());
        Assert.Equal(66.7m, resumo.GetProperty("pontualidadePct").GetDecimal());
        Assert.Equal(10d, resumo.GetProperty("esperaMediaMin").GetDouble());
        Assert.Equal(45d, resumo.GetProperty("descargaMediaMin").GetDouble());
        Assert.True(resumo.GetProperty("temposEstimados").GetBoolean());

        var xlsx = await client.SendAsync(Req($"/v1/Relatorio/agendamentos?formato=Xlsx&{periodo}", tokenA));
        Assert.Equal(HttpStatusCode.OK, xlsx.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsx.Content.Headers.ContentType!.MediaType);
        var bytes = await xlsx.Content.ReadAsByteArrayAsync();
        Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));

        var csv = await client.SendAsync(Req($"/v1/Relatorio/agendamentos?formato=Csv&{periodo}", tokenA));
        var texto = await csv.Content.ReadAsStringAsync();
        Assert.Equal(6, texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("Não compareceu", texto);

        var invalido = await client.SendAsync(Req($"{url}&ordenarPor=inexistente", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    private async Task<Guid> SemearAsync(Guid empresaId, DateOnly dia)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
        var fornecedor = TestDataSeeder.NovoFornecedor(empresaId);
        fornecedor.Nome = "Cooperativa Sul";
        var grade = TestDataSeeder.NovaGrade(empresaId, produto.Id, produto, doca.Id, doca, unidade.Id, dia, dia, new TimeOnly(8, 0), new TimeOnly(16, 0), 60);
        db.UnidadeEntrega.Add(unidade);
        db.LocalDescarga.Add(doca);
        db.Produto.Add(produto);
        db.Fornecedor.Add(fornecedor);
        db.Grade.Add(grade);
        await db.SaveChangesAsync();

        var oito = RelatorioBiPeriodo.InicioDoDiaUtc(dia).AddHours(8);

        Agendamento Slot(int hora, StatusAgendamento status, string? placa, bool daGrade = true)
        {
            var a = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, oito.AddHours(hora - 8), oito.AddHours(hora - 7), status);
            a.PlacaVeiculo = placa;
            a.GradeId = daGrade ? grade.Id : null;
            return a;
        }

        var primeiro = Slot(8, StatusAgendamento.Finalizado, "DET1A11");
        primeiro.ChegouEm = oito.AddMinutes(-10);
        primeiro.FimDescargaEm = oito.AddMinutes(40);

        var segundo = Slot(9, StatusAgendamento.Finalizado, "DET2B22");
        segundo.ChegouEm = oito.AddMinutes(20);
        segundo.FimDescargaEm = oito.AddMinutes(80);

        var pelaGrade = Slot(12, StatusAgendamento.EmAndamento, "DET4D44");
        pelaGrade.LocalDescargaId = null;
        pelaGrade.ChegouEm = oito.AddHours(5).AddMinutes(10);

        var avulso = Slot(14, StatusAgendamento.Agendado, "DET5E55", daGrade: false);
        avulso.FornecedorId = fornecedor.Id;

        db.Agendamento.AddRange(
            primeiro,
            segundo,
            Slot(10, StatusAgendamento.Expirado, "DET3C33"),
            Slot(11, StatusAgendamento.Expirado, null),
            pelaGrade,
            avulso);
        await db.SaveChangesAsync();

        return doca.Id;
    }

    private static async Task<JsonElement> Get(HttpClient client, string url, string token)
    {
        var resposta = await client.SendAsync(Req(url, token));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> Total(HttpClient client, string url, string token) =>
        (await Get(client, url, token)).GetProperty("totalCount").GetInt32();

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Detal {slug} {sufixo} LTDA",
            nomeFantasia = $"Detal {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@detal.test",
            nomeAdmin = "Admin Detal",
            username = $"admin-detal-{sufixo}",
            emailAdmin = $"admin-{sufixo}@detal.test",
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
