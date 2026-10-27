using TruckFlow.Application.Relatorios;
using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class ProdutividadeCalculoTests
{
    private static readonly Guid DocaA = Guid.NewGuid();
    private static readonly Guid DocaB = Guid.NewGuid();
    private static readonly DateOnly Segunda = new(2026, 10, 19);

    private static DateTime Local(DateOnly dia, int hora, int minuto = 0) =>
        DateTime.SpecifyKind(dia.ToDateTime(new TimeOnly(hora, minuto)).AddHours(3), DateTimeKind.Utc);

    private static RelatorioBiLinha Linha(
        DateOnly dia,
        int horaJanela,
        Guid doca,
        (int H, int M)? chegou,
        (int H, int M)? fim,
        StatusAgendamento status = StatusAgendamento.Finalizado) =>
        new(Guid.NewGuid(), status, Local(dia, horaJanela), Local(dia, horaJanela + 1), 30000m,
            null, "Cerealista Oeste", null, "Soja", null, "Unidade Norte",
            doca, doca == DocaA ? "Doca A" : "Doca B",
            chegou.HasValue ? Local(dia, chegou.Value.H, chegou.Value.M) : null,
            null,
            fim.HasValue ? Local(dia, fim.Value.H, fim.Value.M) : null,
            null,
            "ABC1D23");

    private static RelatorioBiPeriodoDto Periodo(DateOnly inicio, DateOnly fim)
    {
        var dias = fim.DayNumber - inicio.DayNumber + 1;
        return new RelatorioBiPeriodoDto { Inicio = inicio, Fim = fim, InicioAnterior = inicio.AddDays(-dias), FimAnterior = inicio.AddDays(-1), Dias = dias };
    }

    private static IReadOnlyDictionary<Guid, List<DateTime>> Fins(IEnumerable<RelatorioBiLinha> linhas) =>
        RelatorioDetalhadoCalculo.FinsPorDoca(linhas
            .Where(l => l.FimDescargaEm.HasValue)
            .Select(l => new FimDescargaDoca(l.LocalDescargaId!.Value, l.FimDescargaEm!.Value)));

    private static List<RelatorioBiLinha> Cenario() =>
    [
        Linha(Segunda, 8, DocaA, (7, 50), (8, 40)),
        Linha(Segunda, 9, DocaA, (8, 20), (9, 20)),
        Linha(Segunda, 10, DocaA, (8, 30), (10, 30)),
        Linha(Segunda, 8, DocaB, (8, 0), (8, 30)),
        Linha(Segunda, 9, DocaB, (9, 0), null, StatusAgendamento.EmAndamento),
        Linha(Segunda.AddDays(-1), 8, DocaA, (8, 0), (8, 40)),
    ];

    [Fact]
    public void Montar_EsperaDescargaPercentisELimite()
    {
        var linhas = Cenario();

        var r = ProdutividadeCalculo.Montar(linhas, Fins(linhas), Periodo(Segunda, Segunda), 30);
        var i = r.Indicadores;

        Assert.Equal(4m, i.Ciclos.Valor);
        Assert.Equal(1m, i.Ciclos.Anterior);
        Assert.Equal(17.5m, i.EsperaMediaMin.Valor);
        Assert.Null(i.EsperaMediaMin.VariacaoPct);
        Assert.Equal(50m, i.EsperaP90Min.Valor);
        Assert.Equal(0m, i.EsperaMedianaMin);
        Assert.Equal(47.5m, i.DescargaMediaMin.Valor);
        Assert.Equal(40m, i.DescargaMediaMin.Anterior);
        Assert.Equal(65m, i.PermanenciaMediaMin.Valor);
        Assert.Equal(1.2m, i.HorasEspera.Valor);
        Assert.Equal(1, i.AcimaDoLimite);
        Assert.Equal(25m, i.AcimaDoLimitePct.Valor);
        Assert.Equal(4m, i.DescargasPorDia);
        Assert.Equal(100m, i.EstimadosPct);
        Assert.Equal(30, r.LimiteEsperaMin);
    }

    [Fact]
    public void Montar_DistribuicaoHeatmapEGrupos()
    {
        var linhas = Cenario();

        var r = ProdutividadeCalculo.Montar(linhas, Fins(linhas), Periodo(Segunda, Segunda), 30);

        Assert.Equal(2, r.Distribuicao.Single(f => f.Rotulo == "até 5 min").Espera);
        Assert.Equal(1, r.Distribuicao.Single(f => f.Rotulo == "15–30 min").Espera);
        Assert.Equal(1, r.Distribuicao.Single(f => f.Rotulo == "45–60 min").Espera);
        Assert.Equal(2, r.Distribuicao.Single(f => f.Rotulo == "30–45 min").Descarga);
        Assert.Equal(1, r.Distribuicao.Single(f => f.Rotulo == "1–1,5 h").Descarga);

        var oito = r.Heatmap.Single(c => c.DiaSemana == 0 && c.Hora == 8);
        Assert.Equal(3, oito.Chegadas);
        Assert.Equal(23.3m, oito.EsperaMediaMin);
        Assert.Equal(3, r.PorHora[8].Chegadas);

        var docaA = r.PorDoca[0];
        Assert.Equal("Doca A", docaA.Nome);
        Assert.Equal(3, docaA.Ciclos);
        Assert.Equal(53.3m, docaA.DescargaMediaMin);
        Assert.Equal(1, docaA.AcimaDoLimite);
        Assert.Equal(0m, docaA.CaixaEspera!.Min);
        Assert.Equal(20m, docaA.CaixaEspera.Mediana);
        Assert.Equal(50m, docaA.CaixaEspera.Max);

        Assert.Equal(4, r.PorProduto.Single().Ciclos);
        Assert.Equal("Cerealista Oeste", r.PorFornecedor.Single().Nome);
        Assert.Single(r.SerieDiaria);
        Assert.Equal(17.5m, r.SerieDiaria[0].EsperaMediaMin);
    }

    [Fact]
    public void Montar_DestacaPiorHorarioLimiteEHorasParadas()
    {
        var linhas = Cenario();

        var r = ProdutividadeCalculo.Montar(linhas, Fins(linhas), Periodo(Segunda, Segunda), 30);

        Assert.Equal("Quem chega segundas às 8h espera 23 min em média", r.Destaques[0].Titulo);
        Assert.Contains(r.Destaques, d => d.Titulo == "1 caminhão esperou mais de 30 min");
        Assert.Contains(r.Destaques, d => d.Titulo == "1,2 h de caminhão parado esperando doca" || d.Titulo == "1.2 h de caminhão parado esperando doca");
        Assert.Contains(r.Destaques, d => d.Titulo.StartsWith("Início da descarga estimado em 100"));
        Assert.DoesNotContain(r.Destaques, d => d.Titulo.Contains("mais devagar"));
    }

    [Fact]
    public void Montar_DestacaDocaLentaEQuedaDaEspera()
    {
        var anterior = Segunda.AddDays(-1);
        var linhas = new List<RelatorioBiLinha>();
        for (var h = 6; h < 9; h++)
        {
            linhas.Add(Linha(Segunda, h, DocaA, (h, 0), (h, 20)));
            linhas.Add(Linha(Segunda, h, DocaB, (h, 0), (h, 50)));
            linhas.Add(Linha(anterior, h, DocaA, (h, 0), (h, 20)));
        }

        linhas.Add(Linha(anterior, 10, DocaA, (8, 0), (10, 20)));

        var r = ProdutividadeCalculo.Montar(linhas, Fins(linhas), Periodo(Segunda, Segunda), 30);

        Assert.Contains(r.Destaques, d => d.Titulo == "Doca B descarrega 43% mais devagar que a média");
        Assert.Contains(r.Destaques, d => d.Titulo.StartsWith("Espera média caiu 100%"));
    }

    [Fact]
    public void Montar_SemCiclos_DevolveVazioSemErro()
    {
        var r = ProdutividadeCalculo.Montar([], Fins([]), Periodo(Segunda, Segunda), 30);

        Assert.Equal(0m, r.Indicadores.Ciclos.Valor);
        Assert.Null(r.Indicadores.EsperaMedianaMin);
        Assert.Empty(r.Destaques);
        Assert.Empty(r.PorDoca);
        Assert.Equal(24, r.PorHora.Count);
        Assert.Equal(9, r.Distribuicao.Count);
    }

    [Theory]
    [InlineData(null, 30)]
    [InlineData(0, 30)]
    [InlineData(15, 15)]
    [InlineData(601, 30)]
    public void ResolverLimite_UsaPadraoForaDaFaixa(int? informado, int esperado)
    {
        Assert.Equal(esperado, ProdutividadeCalculo.ResolverLimite(informado));
    }

    [Fact]
    public void Pico_ContaCaminhoesSimultaneosNoPatio()
    {
        var t = Local(Segunda, 8);
        var (pico, em) = ProdutividadeCalculo.Pico(
        [
            (t, t.AddMinutes(60)),
            (t.AddMinutes(10), t.AddMinutes(30)),
            (t.AddMinutes(20), t.AddMinutes(40)),
            (t.AddMinutes(60), t.AddMinutes(90)),
        ]);

        Assert.Equal(3, pico);
        Assert.Equal(t.AddMinutes(20), em);
    }

    [Fact]
    public void MontarPatio_AgrupaPorDocaEMostraQuemAindaEstaNoPatio()
    {
        var linhas = Cenario();
        var agora = Local(Segunda, 9, 30);

        var patio = ProdutividadeCalculo.MontarPatio(linhas, Fins(linhas), Segunda, agora);

        Assert.Equal(agora, patio.AgoraUtc);
        Assert.Equal(5, patio.Caminhoes);
        Assert.Equal(["Doca A", "Doca B"], patio.Docas.Select(d => d.Nome));
        Assert.Equal(3, patio.Docas[0].Caminhoes.Count);

        var noPatio = patio.Docas[1].Caminhoes.Single(c => c.Status == "EmAndamento");
        Assert.Null(noPatio.FimDescarga);
        Assert.Null(noPatio.InicioDescarga);

        var terceiro = patio.Docas[0].Caminhoes[2];
        Assert.True(terceiro.InicioEstimado);
        Assert.Equal(Local(Segunda, 9, 20), terceiro.InicioDescarga);
        Assert.Equal(50m, terceiro.EsperaMin);

        Assert.Equal(3, patio.PicoSimultaneos);
        Assert.Equal(Local(Segunda, 8, 20), patio.PicoEm);
        Assert.Equal(Local(Segunda, 7), patio.InicioEixo);
        Assert.Equal(Local(Segunda, 11), patio.FimEixo);
    }

    [Fact]
    public void MontarPatio_DiaPassadoNaoTemAgora()
    {
        var linhas = Cenario();

        var patio = ProdutividadeCalculo.MontarPatio(linhas, Fins(linhas), Segunda, Local(Segunda.AddDays(3), 10));

        Assert.Null(patio.AgoraUtc);
        Assert.Empty(ProdutividadeCalculo.MontarPatio([], Fins([]), Segunda, Local(Segunda, 10)).Docas);
    }
}
