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
public class RelatorioCapacidadeDocasHttpTests : IntegrationTestBase
{
    public RelatorioCapacidadeDocasHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task CapacidadeDocas_VagasDaGradeReservasEForaDaGrade_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone));
        var dia = hoje.AddDays(-2);

        var docaA = await SemearAsync(empresaA, dia);
        await SemearAsync(empresaB, dia);

        var url = $"/v1/Relatorio/capacidade-docas?inicio={hoje.AddDays(-6):yyyy-MM-dd}&fim={hoje:yyyy-MM-dd}";
        var resposta = await client.SendAsync(Req(url, tokenA));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var r = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        var ind = r.GetProperty("indicadores");
        Assert.Equal(3m, ind.GetProperty("horasOfertadas").GetProperty("valor").GetDecimal());
        Assert.Equal(66.7m, ind.GetProperty("taxaReservaPct").GetProperty("valor").GetDecimal());
        Assert.Equal(22.2m, ind.GetProperty("utilizacaoRealPct").GetProperty("valor").GetDecimal());
        Assert.Equal(1m, ind.GetProperty("foraDaGrade").GetProperty("valor").GetDecimal());
        Assert.Equal(1m, ind.GetProperty("horasNaoComparecimento").GetProperty("valor").GetDecimal());
        Assert.Equal(2, ind.GetProperty("janelasEsgotadas").GetInt32());
        Assert.Equal(3, ind.GetProperty("janelasComOferta").GetInt32());
        Assert.Equal(40m, ind.GetProperty("descargaMediaMin").GetDecimal());

        var docas = r.GetProperty("docas");
        Assert.Equal(1, docas.GetArrayLength());
        Assert.Equal(docaA, docas[0].GetProperty("id").GetGuid());
        Assert.Equal(3, docas[0].GetProperty("vagasOfertadas").GetInt32());
        Assert.Equal(7, docas[0].GetProperty("serieTaxaReserva").GetArrayLength());

        var comFornecedor = await client.SendAsync(Req($"{url}&fornecedorId={Guid.NewGuid()}", tokenA));
        var rf = await comFornecedor.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3m, rf.GetProperty("indicadores").GetProperty("horasOfertadas").GetProperty("valor").GetDecimal());

        var outraDoca = await client.SendAsync(Req($"{url}&localDescargaId={Guid.NewGuid()}", tokenA));
        var ro = await outraDoca.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0m, ro.GetProperty("indicadores").GetProperty("horasOfertadas").GetProperty("valor").GetDecimal());
        Assert.Equal(0, ro.GetProperty("docas").GetArrayLength());

        var listaDoca = await client.SendAsync(Req($"/v1/AgendamentoAdmin?dataInicio={dia:yyyy-MM-dd}&dataFim={dia:yyyy-MM-dd}&localDescargaId={docaA}&ocultarVagasLivres=true", tokenA));
        Assert.Equal(HttpStatusCode.OK, listaDoca.StatusCode);
        Assert.Equal(3, (await listaDoca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());

        var listaOutraDoca = await client.SendAsync(Req($"/v1/AgendamentoAdmin?dataInicio={dia:yyyy-MM-dd}&dataFim={dia:yyyy-MM-dd}&localDescargaId={Guid.NewGuid()}", tokenA));
        Assert.Equal(0, (await listaOutraDoca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());

        var invertido = await client.SendAsync(Req($"/v1/Relatorio/capacidade-docas?inicio={hoje:yyyy-MM-dd}&fim={hoje.AddDays(-1):yyyy-MM-dd}", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
    }

    private async Task<Guid> SemearAsync(Guid empresaId, DateOnly dia)
    {
        var contexto = ServiceFactory.NewEmpresaContext();
        using var tenant = contexto.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(contexto);

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresaId);
        var doca = TestDataSeeder.NovoLocalDescarga(empresaId, unidade.Id, unidade);
        var produto = TestDataSeeder.NovoProduto(empresaId, doca.Id, doca);
        var grade = TestDataSeeder.NovaGrade(empresaId, produto.Id, produto, doca.Id, doca, unidade.Id, dia, dia, new TimeOnly(8, 0), new TimeOnly(12, 0), 60);
        db.UnidadeEntrega.Add(unidade);
        db.LocalDescarga.Add(doca);
        db.Produto.Add(produto);
        db.Grade.Add(grade);
        await db.SaveChangesAsync();

        var oito = RelatorioBiPeriodo.InicioDoDiaUtc(dia).AddHours(8);

        Agendamento Slot(int hora, StatusAgendamento status, string? placa, bool daGrade = true, int duracao = 60)
        {
            var a = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, oito.AddHours(hora - 8), oito.AddHours(hora - 8).AddMinutes(duracao), status);
            a.PlacaVeiculo = placa;
            a.GradeId = daGrade ? grade.Id : null;
            return a;
        }

        var finalizado = Slot(8, StatusAgendamento.Finalizado, "CAP1A00");
        finalizado.ChegouEm = oito.AddMinutes(5);
        finalizado.FimDescargaEm = oito.AddMinutes(45);

        db.Agendamento.AddRange(
            finalizado,
            Slot(9, StatusAgendamento.Expirado, "CAP2B00"),
            Slot(10, StatusAgendamento.Expirado, null),
            Slot(11, StatusAgendamento.Cancelado, null),
            Slot(13, StatusAgendamento.Finalizado, "CAP3C00", daGrade: false, duracao: 30));
        await db.SaveChangesAsync();

        return doca.Id;
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Capac {slug} {sufixo} LTDA",
            nomeFantasia = $"Capac {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@capac.test",
            nomeAdmin = "Admin Capac",
            username = $"admin-capac-{sufixo}",
            emailAdmin = $"admin-{sufixo}@capac.test",
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
