using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class RelatorioRecebimentosHttpTests : IntegrationTestBase
{
    public RelatorioRecebimentosHttpTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Recebimentos_NotaRecebidoPlanejamentoEOrfaos_SoDaEmpresaLogada()
    {
        await using var factory = new TruckFlowApiFactory(Fixture.ConnectionString);
        using var client = factory.CreateApiClient();

        var (empresaA, tokenA) = await RegistrarEmpresaAsync(client, "a");
        var (empresaB, _) = await RegistrarEmpresaAsync(client, "b");

        await SemearAsync(empresaA, conferido: 24500m, semConferencia: 20000m);
        await SemearAsync(empresaB, conferido: 99000m, semConferencia: 99000m);

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Grade.OperationalTimeZone));
        var resposta = await client.SendAsync(Req($"/v1/Relatorio/recebimentos?inicio={hoje.AddDays(-6):yyyy-MM-dd}&fim={hoje:yyyy-MM-dd}", tokenA));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var r = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        var ind = r.GetProperty("indicadores");
        Assert.Equal(45000m, ind.GetProperty("pesoNotaKg").GetProperty("valor").GetDecimal());
        Assert.Equal(2, ind.GetProperty("descargas").GetProperty("valor").GetDecimal());
        Assert.Equal(25000m, ind.GetProperty("pesoNotaConferidoKg").GetDecimal());
        Assert.Equal(24500m, ind.GetProperty("pesoRecebidoKg").GetDecimal());
        Assert.Equal(-500m, ind.GetProperty("divergenciaKg").GetDecimal());
        Assert.Equal(50.0m, ind.GetProperty("coberturaConferenciaPct").GetDecimal());
        Assert.Equal(1, ind.GetProperty("orfaos").GetInt32());
        Assert.Equal("dia", r.GetProperty("agrupamento").GetString());

        var planejamento = r.GetProperty("planejamento");
        Assert.Equal(1, planejamento.GetArrayLength());
        Assert.Equal(24500m, planejamento[0].GetProperty("recebidoKg").GetDecimal());

        Assert.Equal(1, r.GetProperty("divergencias").GetArrayLength());
        Assert.Equal(45000m, r.GetProperty("matriz").GetProperty("totalGeral").GetDecimal());

        var invalido = await client.SendAsync(Req("/v1/Relatorio/recebimentos?agrupamento=ano", tokenA));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    private async Task SemearAsync(Guid empresaId, decimal conferido, decimal semConferencia)
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

        var planejamento = new PlanejamentoRecebimento
        {
            EmpresaId = empresaId,
            Fornecedor = fornecedor,
            FornecedorId = fornecedor.Id,
            DataInicio = DateTime.UtcNow.AddDays(-10),
            DataFim = DateTime.UtcNow.AddDays(10),
        };
        var item = new ItemPlanejamento
        {
            EmpresaId = empresaId,
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = planejamento,
            PlanejamentoRecebimentoId = planejamento.Id,
            QuantidadeTotalPlanejada = 500000m,
            CadenciaDiariaPlanejada = 25000m,
        };
        db.PlanejamentosRecebimento.Add(planejamento);
        db.ItensPlanejamento.Add(item);
        await db.SaveChangesAsync();

        var inicio = DateTime.UtcNow.AddHours(-1);
        var comConferencia = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, inicio, inicio.AddMinutes(20), StatusAgendamento.Finalizado);
        comConferencia.PlacaVeiculo = "CON1F00";
        comConferencia.VolumeCarga = 25000m;
        comConferencia.FornecedorId = fornecedor.Id;
        var semConf = TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, doca.Id, unidade.Id, produto.Id, inicio.AddMinutes(25), inicio.AddMinutes(45), StatusAgendamento.Finalizado);
        semConf.PlacaVeiculo = "SEM1C00";
        semConf.VolumeCarga = semConferencia;
        semConf.FornecedorId = fornecedor.Id;
        db.Agendamento.AddRange(comConferencia, semConf);
        await db.SaveChangesAsync();

        db.RecebimentoEvento.Add(new RecebimentoEvento(item, conferido, comConferencia.Id, "Confirmação", empresaId, TipoMovimentoRecebimento.Confirmacao));
        db.RecebimentoEvento.Add(RecebimentoEvento.CriarOrfao(produto.Id, fornecedor.Id, 3000m, null, "Órfão", empresaId, TipoMovimentoRecebimento.Confirmacao));
        await db.SaveChangesAsync();
    }

    private static async Task<(Guid EmpresaId, string Token)> RegistrarEmpresaAsync(HttpClient client, string slug)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var resposta = await client.PostAsJsonAsync("/v1/registro", new
        {
            razaoSocial = $"Receb {slug} {sufixo} LTDA",
            nomeFantasia = $"Receb {slug} {sufixo}",
            cnpj = TestDataSeeder.NovoCnpj(),
            emailEmpresa = $"contato-{sufixo}@receb.test",
            nomeAdmin = "Admin Receb",
            username = $"admin-receb-{sufixo}",
            emailAdmin = $"admin-{sufixo}@receb.test",
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
