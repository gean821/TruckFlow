using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class VisaoGeralCalculoTests
{
    private static readonly Guid FornecedorA = Guid.NewGuid();
    private static readonly Guid FornecedorB = Guid.NewGuid();

    private static readonly RelatorioBiPeriodoDto Periodo = new()
    {
        Inicio = new DateOnly(2026, 10, 20),
        Fim = new DateOnly(2026, 10, 20),
        InicioAnterior = new DateOnly(2026, 10, 19),
        FimAnterior = new DateOnly(2026, 10, 19),
        Dias = 1,
    };

    private static RelatorioBiLinha Linha(
        StatusAgendamento status,
        DateTime inicioUtc,
        decimal peso = 0,
        Guid? fornecedor = null,
        int duracaoMin = 60) =>
        new(Guid.NewGuid(), status, inicioUtc, inicioUtc.AddMinutes(duracaoMin), peso,
            fornecedor ?? FornecedorA, fornecedor == FornecedorB ? "Fornecedor B" : "Fornecedor A",
            null, "Soja", null, "Unidade", null, "Doca 1", null, null, null, null);

    private static DateTime Utc(int dia, int hora, int minuto = 0) => new(2026, 10, dia, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void Montar_IndicadoresComparecimentoEMeta()
    {
        var linhas = new List<RelatorioBiLinha>
        {
            Linha(StatusAgendamento.Finalizado, Utc(20, 12), 10000),
            Linha(StatusAgendamento.Finalizado, Utc(20, 13), 10000),
            Linha(StatusAgendamento.Finalizado, Utc(20, 14), 10000),
            Linha(StatusAgendamento.Expirado, Utc(20, 15)),
            Linha(StatusAgendamento.Cancelado, Utc(20, 16)),
            Linha(StatusAgendamento.Finalizado, Utc(19, 12), 10000),
            Linha(StatusAgendamento.Finalizado, Utc(19, 13), 10000),
        };

        var resultado = VisaoGeralCalculo.Montar(linhas, Periodo, metaDiariaKg: 20000, docasAtivas: 1);
        var ind = resultado.Indicadores;

        Assert.Equal(5, ind.Agendamentos.Valor);
        Assert.Equal(3, ind.Concluidos.Valor);
        Assert.Equal(2, ind.Concluidos.Anterior);
        Assert.Equal(50.0m, ind.Concluidos.VariacaoPct);
        Assert.Equal(30000, ind.PesoRecebidoKg.Valor);
        Assert.Equal(75.0m, ind.ComparecimentoPct.Valor);
        Assert.Equal(1, ind.NaoCompareceu.Valor);
        Assert.Equal(1, ind.Cancelados.Valor);
        Assert.Equal(10000, ind.PesoMedioKg.Valor);
        Assert.Equal(150.0m, ind.AtingimentoMetaPct);
        Assert.Single(resultado.SerieDiaria);
        Assert.Equal(new[] { 30000m }, ind.PesoRecebidoKg.Serie);

        Assert.Equal(4, resultado.Funil.Reservados);
        Assert.Equal(3, resultado.Funil.Chegaram);
        Assert.Equal(3, resultado.Funil.Concluidos);
    }

    [Fact]
    public void Montar_HeatmapEOcupacaoUsamHorarioDeBrasilia()
    {
        var linhas = new List<RelatorioBiLinha>
        {
            Linha(StatusAgendamento.Agendado, Utc(20, 12, 30), duracaoMin: 60),
        };

        var resultado = VisaoGeralCalculo.Montar(linhas, Periodo, null, docasAtivas: 1);

        Assert.Equal(1, resultado.HeatmapDiaHora[1][9]);
        Assert.Equal(50.0m, resultado.OcupacaoPorHora[9].OcupacaoPct);
        Assert.Equal(50.0m, resultado.OcupacaoPorHora[10].OcupacaoPct);
        Assert.Equal(1, resultado.OcupacaoPorHora[9].Agendamentos);
        Assert.Contains(resultado.Destaques, d => d.Titulo == "Pico nas terças às 9h");
    }

    [Fact]
    public void Montar_UmFornecedorSo_NaoGeraDestaqueObvioDeConcentracao()
    {
        var linhas = new List<RelatorioBiLinha>
        {
            Linha(StatusAgendamento.Finalizado, Utc(20, 12), 30000, FornecedorA),
        };

        var resultado = VisaoGeralCalculo.Montar(linhas, Periodo, null, docasAtivas: 1);

        Assert.DoesNotContain(resultado.Destaques, d => d.Titulo.Contains("concentrou"));
    }

    [Fact]
    public void Montar_RankingEDestaqueDeConcentracao()
    {
        var linhas = new List<RelatorioBiLinha>
        {
            Linha(StatusAgendamento.Finalizado, Utc(20, 12), 30000, FornecedorA),
            Linha(StatusAgendamento.Finalizado, Utc(20, 13), 10000, FornecedorB),
        };

        var resultado = VisaoGeralCalculo.Montar(linhas, Periodo, null, docasAtivas: 2);

        Assert.Equal("Fornecedor A", resultado.TopFornecedores[0].Nome);
        Assert.Equal(75.0m, resultado.TopFornecedores[0].Percentual);
        Assert.Contains(resultado.Destaques, d => d.Titulo == "Fornecedor A concentrou 75% do volume");
        Assert.Null(resultado.Indicadores.AtingimentoMetaPct);
    }
}
