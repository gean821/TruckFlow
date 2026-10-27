using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class DashboardHojeCalculoTests
{
    private static readonly Guid Doca1 = Guid.NewGuid();
    private static readonly Guid Doca2 = Guid.NewGuid();
    private static readonly DateTime Agora = Utc(20, 13);

    private static readonly RelatorioBiDoca[] Docas =
    [
        new(Doca1, "Doca 1", "Unidade"),
        new(Doca2, "Doca 2", "Unidade"),
    ];

    private static DateTime Utc(int dia, int hora, int minuto = 0) => new(2026, 10, dia, hora, minuto, 0, DateTimeKind.Utc);

    private static RelatorioBiLinha Linha(
        StatusAgendamento status,
        DateTime inicio,
        Guid doca,
        decimal peso = 0,
        DateTime? chegou = null,
        DateTime? fim = null,
        string placa = "ABC1D23") =>
        new(Guid.NewGuid(), status, inicio, inicio.AddMinutes(40), peso, null, "Fornecedor", null, "Soja",
            null, "Unidade", doca, doca == Doca1 ? "Doca 1" : "Doca 2", chegou, null, fim, null, placa, "João");

    private static List<RelatorioBiLinha> Cenario() =>
    [
        Linha(StatusAgendamento.Finalizado, Utc(20, 11), Doca1, 30000, chegou: Utc(20, 10, 50), fim: Utc(20, 11, 40)),
        Linha(StatusAgendamento.EmAndamento, Utc(20, 12, 20), Doca2, chegou: Utc(20, 12, 30), placa: "PAT1O00"),
        Linha(StatusAgendamento.Agendado, Utc(20, 12), Doca2, placa: "ATR4S00"),
        Linha(StatusAgendamento.Agendado, Utc(20, 14), Doca1, placa: "PRX1M00"),
        Linha(StatusAgendamento.Agendado, Utc(20, 18), Doca1),
        Linha(StatusAgendamento.Cancelado, Utc(20, 15), Doca1),
        Linha(StatusAgendamento.Finalizado, Utc(13, 11), Doca1, 20000),
        Linha(StatusAgendamento.Finalizado, Utc(13, 17), Doca1, 20000),
    ];

    [Fact]
    public void Montar_IndicadoresDoDia_ComparadosComAMesmaHoraDasUltimasSemanas()
    {
        var resultado = DashboardHojeCalculo.Montar(Cenario(), Docas, Agora, metaDiariaKg: 60000);
        var ind = resultado.Indicadores;

        Assert.Equal(new DateOnly(2026, 10, 20), resultado.Dia);
        Assert.Equal(10, resultado.HoraAtual);
        Assert.Equal("terça-feira", resultado.DiaSemana);

        Assert.Equal(5, ind.AgendadosHoje.Valor);
        Assert.Equal(2, ind.AgendadosHoje.Referencia);
        Assert.Equal(1, ind.Concluidos.Valor);
        Assert.Equal(1, ind.Concluidos.Referencia);
        Assert.Equal(0, ind.Concluidos.VariacaoPct);
        Assert.Equal(30000, ind.PesoRecebidoKg.Valor);
        Assert.Equal(20000, ind.PesoRecebidoKg.Referencia);
        Assert.Equal(1, ind.NoPatio.Valor);
        Assert.Equal(1, ind.Atrasados.Valor);
        Assert.Equal(2, ind.AindaPorVir);
        Assert.Equal(50.0m, ind.AtingimentoMetaPct);
    }

    [Fact]
    public void Montar_DocasPatioEChegadas()
    {
        var resultado = DashboardHojeCalculo.Montar(Cenario(), Docas, Agora, null);

        var doca1 = resultado.Docas.Single(d => d.Id == Doca1);
        Assert.False(doca1.Ocupada);
        Assert.Equal("PRX1M00", doca1.Proximo!.Placa);
        Assert.Equal(60, doca1.Proximo.Minutos);
        Assert.Equal(1, doca1.ConcluidosHoje);

        var doca2 = resultado.Docas.Single(d => d.Id == Doca2);
        Assert.True(doca2.Ocupada);
        Assert.Equal("PAT1O00", doca2.Atual!.Placa);
        Assert.Equal(30, doca2.Atual.Minutos);

        Assert.Equal(1, resultado.Patio.NoPatio);
        Assert.Equal(50, resultado.Patio.PermanenciaMediaMin);
        Assert.Equal("PAT1O00", resultado.Patio.MaisAntigo!.Placa);

        Assert.Equal("PRX1M00", Assert.Single(resultado.ProximasChegadas).Placa);
        var atrasado = Assert.Single(resultado.Atrasados);
        Assert.Equal("ATR4S00", atrasado.Placa);
        Assert.Equal(60, atrasado.Minutos);

        Assert.Equal(1, resultado.PorHora[8].Agendados);
        Assert.Equal(1, resultado.PorHora[7].Chegadas);
        Assert.Equal(1, resultado.PorHora[8].Concluidos);
        Assert.Equal(1, resultado.PorHora[8].MediaAgendados);
    }

    [Fact]
    public void Montar_SemHistorico_NaoInventaReferencia()
    {
        var resultado = DashboardHojeCalculo.Montar([Linha(StatusAgendamento.Agendado, Utc(20, 18), Doca1)], Docas, Agora, null);

        Assert.Null(resultado.Indicadores.AgendadosHoje.Referencia);
        Assert.Null(resultado.Indicadores.AgendadosHoje.VariacaoPct);
        Assert.Null(resultado.Indicadores.AtingimentoMetaPct);
        Assert.Null(resultado.Patio.PermanenciaMediaMin);
    }
}
