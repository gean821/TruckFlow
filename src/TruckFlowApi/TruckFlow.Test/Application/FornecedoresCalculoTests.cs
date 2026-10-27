using TruckFlow.Application.Relatorios;
using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class FornecedoresCalculoTests
{
    private static readonly Guid FornA = Guid.NewGuid();
    private static readonly Guid FornB = Guid.NewGuid();
    private static readonly Guid FornC = Guid.NewGuid();
    private static readonly Guid Doca = Guid.NewGuid();
    private static readonly DateOnly Segunda = new(2026, 10, 19);

    private static DateTime Local(DateOnly dia, int hora, int minuto = 0) =>
        DateTime.SpecifyKind(dia.ToDateTime(new TimeOnly(hora, minuto)).AddHours(3), DateTimeKind.Utc);

    private static RelatorioBiLinha Linha(DateOnly dia, int hora, Guid fornecedor, StatusAgendamento status, int? chegouMin = null, decimal peso = 30000m)
    {
        var inicio = Local(dia, hora);
        var nome = fornecedor == FornA ? "Cooperativa A" : fornecedor == FornB ? "Fazenda B" : "Sítio C";
        return new(Guid.NewGuid(), status, inicio, inicio.AddMinutes(60), peso,
            fornecedor, nome, null, fornecedor == FornA ? "Soja" : "Milho", null, "Unidade Norte", Doca, "Doca 1",
            chegouMin.HasValue ? inicio.AddMinutes(chegouMin.Value) : null,
            null,
            chegouMin.HasValue && status == StatusAgendamento.Finalizado ? inicio.AddMinutes(chegouMin.Value + 40) : null,
            null,
            "ABC1D23");
    }

    private static RelatorioBiPeriodoDto Periodo(DateOnly inicio, DateOnly fim)
    {
        var dias = fim.DayNumber - inicio.DayNumber + 1;
        return new RelatorioBiPeriodoDto { Inicio = inicio, Fim = fim, InicioAnterior = inicio.AddDays(-dias), FimAnterior = inicio.AddDays(-1), Dias = dias };
    }

    private static List<RelatorioBiLinha> Cenario()
    {
        var domingo = Segunda.AddDays(-1);
        return
        [
            Linha(Segunda, 6, FornA, StatusAgendamento.Finalizado, -10),
            Linha(Segunda, 8, FornA, StatusAgendamento.Finalizado, -40),
            Linha(Segunda, 10, FornA, StatusAgendamento.Finalizado, 15),
            Linha(Segunda, 12, FornA, StatusAgendamento.Finalizado, 80),
            Linha(Segunda, 14, FornA, StatusAgendamento.Expirado),
            Linha(Segunda, 15, FornA, StatusAgendamento.Cancelado),
            Linha(Segunda, 7, FornB, StatusAgendamento.Finalizado, 5, 20000m),
            Linha(Segunda, 9, FornB, StatusAgendamento.Finalizado, 5, 20000m),
            Linha(Segunda, 11, FornB, StatusAgendamento.Finalizado, 5, 20000m),
            Linha(Segunda, 16, FornC, StatusAgendamento.Agendado),
            Linha(domingo, 8, FornA, StatusAgendamento.Finalizado, 0),
            Linha(domingo, 10, FornA, StatusAgendamento.Finalizado, 0),
        ];
    }

    private static IReadOnlyDictionary<Guid, List<DateTime>> SemFins() => RelatorioDetalhadoCalculo.FinsPorDoca([]);

    [Fact]
    public void Medir_PontualidadeNoShowCancelamentoEScore()
    {
        var m = FornecedoresCalculo.Medir(Cenario().Where(l => l.FornecedorId == FornA && l.DataInicio >= Local(Segunda, 0)).ToList());

        Assert.Equal(6, m.Agendamentos);
        Assert.Equal(5, m.Realizaveis);
        Assert.Equal(4, m.Concluidos);
        Assert.Equal(120000m, m.PesoKg);
        Assert.Equal(4, m.Chegaram);
        Assert.Equal(2, m.Adiantados);
        Assert.Equal(1, m.NoHorario);
        Assert.Equal(1, m.Atrasados);
        Assert.Equal(80m, m.AtrasoMedioMin);
        Assert.Equal(75m, m.PontualidadePct);
        Assert.Equal(20m, m.NoShowPct);
        Assert.Equal(16.7m, m.CancelamentoPct);
        Assert.Equal(78m, m.Score);
    }

    [Theory]
    [InlineData(null, "Poucos dados")]
    [InlineData(90, "Excelente")]
    [InlineData(70, "Bom")]
    [InlineData(55, "Atenção")]
    [InlineData(10, "Crítico")]
    public void Classificar_PorFaixaDeScore(int? score, string esperado)
    {
        Assert.Equal(esperado, FornecedoresCalculo.Classificar(score));
    }

    [Fact]
    public void Montar_RankingIndicadoresEPoucosDados()
    {
        var r = FornecedoresCalculo.Montar(Cenario(), SemFins(), Periodo(Segunda, Segunda), "dia");

        Assert.Equal(["Cooperativa A", "Fazenda B", "Sítio C"], r.Ranking.Select(f => f.Nome));
        var a = r.Ranking[0];
        Assert.Equal(66.7m, a.PctVolume);
        Assert.Equal(78m, a.Score);
        Assert.Equal("Bom", a.Classificacao);
        Assert.Equal(100m, a.PontualidadeAnteriorPct);
        Assert.Equal(-25m, a.VariacaoPontualidade);
        Assert.Equal("Soja", a.ProdutoPrincipal);
        Assert.Equal(40m, a.DescargaMediaMin);
        Assert.Single(a.SeriePontualidade);

        Assert.Equal(100m, r.Ranking[1].Score);
        Assert.Equal("Excelente", r.Ranking[1].Classificacao);
        Assert.Null(r.Ranking[2].Score);
        Assert.Equal("Poucos dados", r.Ranking[2].Classificacao);

        var i = r.Indicadores;
        Assert.Equal(3m, i.FornecedoresAtivos.Valor);
        Assert.Equal(1m, i.FornecedoresAtivos.Anterior);
        Assert.Equal(85.7m, i.PontualidadePct.Valor);
        Assert.Equal(12.5m, i.NoShowPct.Valor);
        Assert.Equal(10m, i.CancelamentoPct.Valor);
        Assert.Equal(80m, i.AtrasoMedioMin.Valor);
        Assert.Equal(180000m, i.PesoRecebidoKg.Valor);
        Assert.Equal(60000m, i.PesoRecebidoKg.Anterior);
        Assert.Equal(89m, i.ScoreMedio);
        Assert.Null(i.ConcentracaoTop3Pct);
        Assert.Equal(0, i.ComScoreCritico);
        Assert.Equal("dia", r.Agrupamento);
        Assert.Single(r.Serie);
    }

    [Fact]
    public void Montar_DestacaMelhorFaltosoAtrasadoEQuemPiorou()
    {
        var r = FornecedoresCalculo.Montar(Cenario(), SemFins(), Periodo(Segunda, Segunda), "dia");

        Assert.Equal("Fazenda B é o fornecedor mais confiável (score 100)", r.Destaques[0].Titulo);
        Assert.Contains(r.Destaques, d => d.Titulo == "Cooperativa A faltou em 20% dos agendamentos");
        Assert.Contains(r.Destaques, d => d.Titulo == "Cooperativa A chegou depois da janela em 25% das vezes");
        Assert.Contains(r.Destaques, d => d.Titulo == "Pontualidade de Cooperativa A caiu 25 pontos");
        Assert.Equal(5, r.Destaques.Count);
    }

    [Fact]
    public void Montar_ConcentracaoComQuatroOuMaisFornecedores()
    {
        var linhas = new List<RelatorioBiLinha>();
        foreach (var (fornecedor, peso) in new[] { (Guid.NewGuid(), 70000m), (Guid.NewGuid(), 10000m), (Guid.NewGuid(), 10000m), (Guid.NewGuid(), 10000m) })
            linhas.Add(Linha(Segunda, 8, fornecedor, StatusAgendamento.Finalizado, 0, peso));

        var r = FornecedoresCalculo.Montar(linhas, SemFins(), Periodo(Segunda, Segunda), "dia");

        Assert.Equal(90m, r.Indicadores.ConcentracaoTop3Pct);
        Assert.Contains(r.Destaques, d => d.Titulo == "3 fornecedores concentram 90% do volume");
    }

    [Fact]
    public void Detalhe_ComparaComAMediaEListaOcorrencias()
    {
        var d = FornecedoresCalculo.Detalhe(Cenario(), SemFins(), Periodo(Segunda, Segunda), "dia", FornA);

        Assert.Equal("Cooperativa A", d.Fornecedor.Nome);
        Assert.Equal(78m, d.Fornecedor.Score);
        Assert.Equal("Todos os fornecedores", d.Media.Nome);
        Assert.Equal(87m, d.Media.Score);
        Assert.Equal(1, d.Posicao);
        Assert.Equal(3, d.TotalFornecedores);

        Assert.Equal(6, d.PorDiaSemana[0].Agendamentos);
        Assert.Equal(0, d.PorDiaSemana[1].Agendamentos);
        Assert.Equal("Soja", d.PorProduto.Single().Nome);

        int Faixa(string rotulo) => d.DistribuicaoChegada.Single(f => f.Rotulo == rotulo).Quantidade;
        Assert.Equal(1, Faixa("30 min a 1 h antes"));
        Assert.Equal(1, Faixa("Até 30 min antes"));
        Assert.Equal(1, Faixa("Dentro da janela"));
        Assert.Equal(1, Faixa("Até 30 min depois"));
        Assert.Equal(0, Faixa("Mais de 1 h depois"));

        Assert.Equal(["Cancelado", "Não compareceu", "Atraso"], d.Ocorrencias.Select(o => o.Tipo));
        Assert.Equal(80m, d.Ocorrencias[2].AtrasoMin);
    }

    [Fact]
    public void Montar_SemFornecedorCadastradoNaoRecebeScoreNemContaComoCritico()
    {
        var inicio = Local(Segunda, 8);
        var semCadastro = Enumerable.Range(0, 4)
            .Select(i => new RelatorioBiLinha(Guid.NewGuid(), StatusAgendamento.Expirado, inicio.AddHours(i), inicio.AddHours(i + 1), 0m,
                null, null, null, null, null, null, Doca, "Doca 1", null, null, null, null))
            .ToList();
        var linhas = Cenario().Concat(semCadastro).ToList();

        var r = FornecedoresCalculo.Montar(linhas, SemFins(), Periodo(Segunda, Segunda), "dia");

        var linha = r.Ranking.Single(f => f.Id is null);
        Assert.Equal("Sem fornecedor", linha.Nome);
        Assert.Null(linha.Score);
        Assert.Equal(FornecedoresCalculo.SemCadastro, linha.Classificacao);
        Assert.Equal(100m, linha.NoShowPct);
        Assert.Equal(0, r.Indicadores.ComScoreCritico);
        Assert.Equal(89m, r.Indicadores.ScoreMedio);
        Assert.Equal(3m, r.Indicadores.FornecedoresAtivos.Valor);
    }

    [Fact]
    public void Montar_SemAgendamentos_DevolveVazioSemErro()
    {
        var r = FornecedoresCalculo.Montar([], SemFins(), Periodo(Segunda, Segunda), "dia");

        Assert.Empty(r.Ranking);
        Assert.Empty(r.Destaques);
        Assert.Null(r.Indicadores.ScoreMedio);
        Assert.Equal(0m, r.Indicadores.PontualidadePct.Valor);
    }
}
