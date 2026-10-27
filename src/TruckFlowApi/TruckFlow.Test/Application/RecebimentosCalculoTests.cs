using TruckFlow.Application.Exceptions;
using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlow.Test.Application;

public class RecebimentosCalculoTests
{
    private static readonly Guid FornA = Guid.NewGuid();
    private static readonly Guid FornB = Guid.NewGuid();

    private static RelatorioBiPeriodoDto Periodo(DateOnly inicio, DateOnly fim)
    {
        var dias = fim.DayNumber - inicio.DayNumber + 1;
        return new RelatorioBiPeriodoDto { Inicio = inicio, Fim = fim, InicioAnterior = inicio.AddDays(-dias), FimAnterior = inicio.AddDays(-1), Dias = dias };
    }

    private static RecebimentoLinha Linha(DateTime inicioUtc, decimal nota, decimal? recebido = null, string produto = "Soja", Guid? fornecedor = null, string unidade = "Unidade Norte") =>
        new(Guid.NewGuid(), inicioUtc, nota, recebido, null, produto, fornecedor ?? FornA,
            fornecedor == FornB ? "Fornecedor B" : "Fornecedor A", null, unidade, "ABC1D23", 123);

    private static DateTime Utc(int mes, int dia, int hora = 15) => new(2026, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null, 30, "dia")]
    [InlineData(null, 90, "semana")]
    [InlineData(null, 300, "mes")]
    [InlineData("SEMANA", 10, "semana")]
    [InlineData(" mes ", 10, "mes")]
    public void ResolverAgrupamento_EscolheOuValida(string? pedido, int dias, string esperado)
    {
        Assert.Equal(esperado, RecebimentosCalculo.ResolverAgrupamento(pedido, dias));
    }

    [Fact]
    public void ResolverAgrupamento_Invalido_ErroEmPortugues()
    {
        var ex = Assert.Throws<BusinessException>(() => RecebimentosCalculo.ResolverAgrupamento("ano", 10));
        Assert.Equal("Agrupamento inválido. Use dia, semana ou mês.", ex.Message);
    }

    [Fact]
    public void Montar_DivergenciaECoberturaSoContamDescargasConferidas()
    {
        var linhas = new List<RecebimentoLinha>
        {
            Linha(Utc(10, 20), 30000, 29700),
            Linha(Utc(10, 21), 20000),
            Linha(Utc(10, 22), 10000, 10100),
            Linha(Utc(10, 12), 5000),
        };

        var r = RecebimentosCalculo.Montar(linhas, [], (2, 1500m), Periodo(new(2026, 10, 19), new(2026, 10, 25)), "dia", 10000, new(2026, 10, 30));
        var i = r.Indicadores;

        Assert.Equal(60000, i.PesoNotaKg.Valor);
        Assert.Equal(5000, i.PesoNotaKg.Anterior);
        Assert.Equal(3, i.Descargas.Valor);
        Assert.Equal(40000, i.PesoNotaConferidoKg);
        Assert.Equal(39800, i.PesoRecebidoKg);
        Assert.Equal(-200, i.DivergenciaKg);
        Assert.Equal(-0.5m, i.DivergenciaPct);
        Assert.Equal(66.7m, i.CoberturaConferenciaPct);
        Assert.Equal(2, i.Orfaos);
        Assert.Equal(1500m, i.OrfaosKg);
        Assert.Equal(70000, i.MetaPeriodoKg);

        Assert.Equal(-300, r.Divergencias[0].DiferencaKg);
        Assert.Equal(-1.0m, r.Divergencias[0].DiferencaPct);
        Assert.Equal(2, r.Divergencias.Count);
    }

    [Fact]
    public void Montar_SemanasEMesesViramBaldesComRotulo()
    {
        var linhas = new List<RecebimentoLinha> { Linha(Utc(10, 20), 1000), Linha(Utc(10, 27), 3000) };

        var semanal = RecebimentosCalculo.Montar(linhas, [], (0, 0), Periodo(new(2026, 10, 19), new(2026, 11, 1)), "semana", 500, new(2026, 11, 5));
        Assert.Equal(2, semanal.Serie.Count);
        Assert.Equal("19/10–25/10", semanal.Serie[0].Rotulo);
        Assert.Equal(1000, semanal.Serie[0].TotalKg);
        Assert.Equal(3500, semanal.Serie[0].MetaKg);

        var mensal = RecebimentosCalculo.Montar(linhas, [], (0, 0), Periodo(new(2026, 9, 15), new(2026, 10, 31)), "mes", null, new(2026, 11, 5));
        Assert.Equal(2, mensal.Serie.Count);
        Assert.Equal("15/09–30/09", mensal.Serie[0].Rotulo);
        Assert.Equal("out/26", mensal.Serie[1].Rotulo);
        Assert.Equal(4000, mensal.Serie[1].PorProduto["Soja"]);
    }

    [Fact]
    public void Montar_PlanejadoContaSoDiasDeOperacaoAteHoje()
    {
        var itens = new List<PlanejamentoItemLinha>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Soja", FornA, "Fornecedor A", Utc(10, 1, 3), Utc(10, 31, 3), 1000, "1,2,3,4,5", 50000, 4500),
            new(Guid.NewGuid(), Guid.NewGuid(), "Milho", FornB, "Fornecedor B", Utc(10, 1, 3), Utc(10, 31, 3), 1000, "", 50000, 0),
        };

        var semanaPassada = RecebimentosCalculo.Montar([], itens, (0, 0), Periodo(new(2026, 10, 19), new(2026, 10, 25)), "dia", null, new(2026, 10, 30));
        var soja = semanaPassada.Planejamento.Single(p => p.Produto == "Soja");
        Assert.Equal(5000, soja.PlanejadoKg);
        Assert.Equal(90.0m, soja.CumprimentoPct);
        Assert.Equal("No ritmo", soja.Situacao);
        var milho = semanaPassada.Planejamento.Single(p => p.Produto == "Milho");
        Assert.Equal(7000, milho.PlanejadoKg);
        Assert.Equal("Atrasado", milho.Situacao);
        Assert.Equal(37.5m, semanaPassada.Indicadores.CumprimentoPlanejamentoPct);

        var semanaAtual = RecebimentosCalculo.Montar([], itens, (0, 0), Periodo(new(2026, 10, 26), new(2026, 11, 1)), "dia", null, new(2026, 10, 27));
        Assert.Equal(2000, semanaAtual.Planejamento.Single(p => p.Produto == "Soja").PlanejadoKg);
    }

    [Fact]
    public void Montar_MatrizComTotaisEFluxoComIdsUnicos()
    {
        var linhas = new List<RecebimentoLinha>
        {
            Linha(Utc(10, 20), 1000, produto: "Soja", fornecedor: FornA),
            Linha(Utc(10, 20, 16), 2000, produto: "Milho", fornecedor: FornA),
            Linha(Utc(10, 21), 4000, produto: "Soja", fornecedor: FornB, unidade: "Unidade Sul"),
        };

        var r = RecebimentosCalculo.Montar(linhas, [], (0, 0), Periodo(new(2026, 10, 19), new(2026, 10, 25)), "dia", null, new(2026, 10, 30));

        Assert.Equal(7000, r.Matriz.TotalGeral);
        Assert.Equal(r.Matriz.TotalGeral, r.Matriz.Linhas.Sum(l => l.Total));
        Assert.Equal(5000, r.Matriz.TotalPorProduto["Soja"]);
        Assert.Equal("Fornecedor B", r.Matriz.Linhas[0].Fornecedor);

        Assert.Equal(r.Fluxo.Nos.Count, r.Fluxo.Nos.Select(n => n.Id).Distinct().Count());
        Assert.Contains(r.Fluxo.Ligacoes, l => l.Origem == "f:Fornecedor B" && l.Destino == "p:Soja" && l.Kg == 4000);
        Assert.Contains(r.Fluxo.Ligacoes, l => l.Origem == "p:Soja" && l.Destino == "u:Unidade Sul");
        Assert.Equal(2, r.PorFornecedor.Count);
        Assert.Equal(57.1m, r.PorFornecedor[0].Percentual);
    }
}
