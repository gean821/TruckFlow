using TruckFlow.Application.RelatoriosBi;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;
using Classe = TruckFlow.Application.RelatoriosBi.CapacidadeDocasCalculo.Classe;

namespace TruckFlow.Test.Application;

public class CapacidadeDocasCalculoTests
{
    private static readonly Guid DocaA = Guid.NewGuid();
    private static readonly Guid DocaB = Guid.NewGuid();
    private static readonly Guid DocaParada = Guid.NewGuid();

    private static RelatorioBiPeriodoDto Periodo(DateOnly inicio, DateOnly fim)
    {
        var dias = fim.DayNumber - inicio.DayNumber + 1;
        return new RelatorioBiPeriodoDto { Inicio = inicio, Fim = fim, InicioAnterior = inicio.AddDays(-dias), FimAnterior = inicio.AddDays(-1), Dias = dias };
    }

    private static DateTime Local(DateOnly dia, int hora, int minuto = 0) =>
        DateTime.SpecifyKind(dia.ToDateTime(new TimeOnly(hora, minuto)).AddHours(3), DateTimeKind.Utc);

    private static CapacidadeVagaLinha Vaga(
        DateOnly dia,
        int hora,
        StatusAgendamento status,
        bool reserva,
        Guid? doca = null,
        bool daGrade = true,
        int duracao = 60,
        DateTime? chegou = null,
        DateTime? fim = null) =>
        new(Guid.NewGuid(), status, Local(dia, hora), Local(dia, hora).AddMinutes(duracao), reserva, daGrade,
            doca ?? DocaA, doca == DocaB ? "Doca B" : "Doca A", "Unidade Norte", chegou, null, fim);

    private static RelatorioBiDoca Doca(Guid id, string nome) => new(id, nome, "Unidade Norte");

    [Theory]
    [InlineData(StatusAgendamento.Disponivel, false, true, Classe.Livre)]
    [InlineData(StatusAgendamento.Expirado, false, true, Classe.Livre)]
    [InlineData(StatusAgendamento.Cancelado, false, true, Classe.ForaDaOferta)]
    [InlineData(StatusAgendamento.Cancelado, true, true, Classe.Livre)]
    [InlineData(StatusAgendamento.Cancelado, true, false, Classe.ForaDaOferta)]
    [InlineData(StatusAgendamento.Agendado, true, true, Classe.Reservada)]
    [InlineData(StatusAgendamento.EmAndamento, true, false, Classe.Realizada)]
    [InlineData(StatusAgendamento.Finalizado, true, true, Classe.Realizada)]
    [InlineData(StatusAgendamento.Expirado, true, true, Classe.NaoCompareceu)]
    public void Classificar_SeparaOfertaReservaUsoENaoComparecimento(StatusAgendamento status, bool reserva, bool daGrade, Classe esperada)
    {
        var dia = new DateOnly(2026, 10, 19);
        Assert.Equal(esperada, CapacidadeDocasCalculo.Classificar(Vaga(dia, 8, status, reserva, daGrade: daGrade)));
    }

    [Fact]
    public void Montar_TaxaDeReservaUtilizacaoRealEOciosidade()
    {
        var dia = new DateOnly(2026, 10, 19);
        var linhas = new List<CapacidadeVagaLinha>
        {
            Vaga(dia, 8, StatusAgendamento.Finalizado, true, chegou: Local(dia, 8, 5), fim: Local(dia, 8, 45)),
            Vaga(dia, 9, StatusAgendamento.Agendado, true),
            Vaga(dia, 10, StatusAgendamento.Expirado, true),
            Vaga(dia, 11, StatusAgendamento.Disponivel, false),
            Vaga(dia, 12, StatusAgendamento.Cancelado, false),
            Vaga(dia, 13, StatusAgendamento.Finalizado, true, daGrade: false, duracao: 30),
        };

        var r = CapacidadeDocasCalculo.Montar(linhas, [Doca(DocaA, "Doca A")], Periodo(dia, dia));
        var i = r.Indicadores;

        Assert.Equal(4m, i.HorasOfertadas.Valor);
        Assert.Equal(75m, i.TaxaReservaPct.Valor);
        Assert.Equal(16.7m, i.UtilizacaoRealPct.Valor);
        Assert.Equal(1m, i.HorasNaoComparecimento.Valor);
        Assert.Equal(1m, i.HorasOciosas.Valor);
        Assert.Equal(1m, i.ForaDaGrade.Valor);
        Assert.Equal(4, i.VagasOfertadas);
        Assert.Equal(3, i.VagasReservadas);
        Assert.Equal(3, i.JanelasEsgotadas);
        Assert.Equal(4, i.JanelasComOferta);
        Assert.Equal(60m, i.VagaMediaMin);
        Assert.Equal(40m, i.DescargaMediaMin);
        Assert.Equal(66.7m, i.AproveitamentoVagaPct);

        var oito = r.HeatmapSemana.Single(c => c.Hora == 8);
        Assert.Equal(0, oito.DiaSemana);
        Assert.Equal(100m, oito.TaxaReservaPct);
        Assert.Equal(66.7m, oito.UtilizacaoRealPct);
        Assert.Equal(1, oito.DiasEsgotados);
        Assert.DoesNotContain(r.HeatmapSemana, c => c.Hora == 12);
        var encaixe = r.HeatmapSemana.Single(c => c.Hora == 13);
        Assert.Equal(0, encaixe.Ofertadas);
        Assert.Equal(1, encaixe.ForaDaGrade);
        Assert.Null(encaixe.TaxaReservaPct);
        Assert.DoesNotContain(r.ExcessoDemanda, j => j.Hora == 13);

        var doca = Assert.Single(r.Docas);
        Assert.Equal(40m, doca.DescargaMediaMin);
        Assert.Equal(1m, doca.HorasOciosas);

        Assert.Contains(r.Recomendacoes, x => x.Titulo == "Descarga média de 40 min em vagas de 60 min");
        Assert.Contains(r.Recomendacoes, x => x.Titulo == "1 agendamento fora da grade");
        Assert.Contains(r.Recomendacoes, x => x.Titulo == "1 h de doca perdidas por não comparecimento");
    }

    [Fact]
    public void Montar_ApontaJanelasEsgotadasEOciosasESugereRedistribuir()
    {
        var segundas = new[] { new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 19) };
        var linhas = segundas
            .SelectMany(dia => new[]
            {
                Vaga(dia, 8, StatusAgendamento.Finalizado, true, DocaA),
                Vaga(dia, 8, StatusAgendamento.Finalizado, true, DocaB),
                Vaga(dia, 15, StatusAgendamento.Expirado, false, DocaA),
                Vaga(dia, 15, StatusAgendamento.Expirado, false, DocaB),
            })
            .ToList();

        var r = CapacidadeDocasCalculo.Montar(linhas, [Doca(DocaA, "Doca A"), Doca(DocaB, "Doca B")], Periodo(segundas[0], segundas[1]));

        var excesso = Assert.Single(r.ExcessoDemanda);
        Assert.Equal("segundas às 8h", excesso.Rotulo);
        Assert.Equal(2, excesso.DiasEsgotados);
        Assert.Equal(2, excesso.DiasComOferta);

        var ociosa = Assert.Single(r.Ociosas);
        Assert.Equal("segundas às 15h", ociosa.Rotulo);
        Assert.Equal(4, ociosa.VagasLivres);
        Assert.Equal(0m, ociosa.TaxaReservaPct);

        Assert.Equal("Segundas às 8h esgotaram em 2 de 2 dias", r.Recomendacoes[0].Titulo);
        Assert.Contains(r.Recomendacoes, x => x.Titulo == "Redistribua vagas de segundas às 15h para segundas às 8h");

        Assert.Equal(4, r.HeatmapDocas.Count);
        Assert.Equal(100m, r.HeatmapDocas.Single(h => h.DocaId == DocaB && h.Hora == 8).TaxaReservaPct);
        Assert.Equal(8, r.SerieDiaria.Count);
        Assert.Equal(50m, r.SerieDiaria[^1].TaxaReservaPct);
        Assert.Null(r.SerieDiaria[1].TaxaReservaPct);
    }

    [Fact]
    public void Montar_DocasOrdenadasPelaReservaEComparacaoComPeriodoAnterior()
    {
        var dia = new DateOnly(2026, 10, 20);
        var anterior = dia.AddDays(-1);
        var linhas = new List<CapacidadeVagaLinha>
        {
            Vaga(dia, 8, StatusAgendamento.Agendado, true, DocaB),
            Vaga(dia, 9, StatusAgendamento.Agendado, true, DocaB),
            Vaga(dia, 8, StatusAgendamento.Agendado, true, DocaA),
            Vaga(dia, 9, StatusAgendamento.Disponivel, false, DocaA),
            Vaga(anterior, 8, StatusAgendamento.Finalizado, true, DocaA),
            Vaga(anterior, 9, StatusAgendamento.Expirado, false, DocaA),
            Vaga(anterior, 10, StatusAgendamento.Expirado, false, DocaA),
            Vaga(anterior, 11, StatusAgendamento.Expirado, false, DocaA),
        };

        var r = CapacidadeDocasCalculo.Montar(
            linhas,
            [Doca(DocaA, "Doca A"), Doca(DocaB, "Doca B"), Doca(DocaParada, "Doca parada")],
            Periodo(dia, dia));

        Assert.Equal(["Doca B", "Doca A", "Doca parada"], r.Docas.Select(d => d.Nome));
        Assert.Equal(100m, r.Docas[0].TaxaReservaPct);
        Assert.Equal(8, r.Docas[0].HoraPico);
        Assert.Null(r.Docas[2].TaxaReservaPct);
        Assert.Equal(0, r.Docas[2].VagasOfertadas);

        Assert.Equal(75m, r.Indicadores.TaxaReservaPct.Valor);
        Assert.Equal(25m, r.Indicadores.TaxaReservaPct.Anterior);
        Assert.Equal(200m, r.Indicadores.TaxaReservaPct.VariacaoPct);
        Assert.Equal(0m, r.Indicadores.UtilizacaoRealPct.Valor);
    }

    [Fact]
    public void Montar_SemVagas_DevolveVazioSemErro()
    {
        var dia = new DateOnly(2026, 10, 20);
        var r = CapacidadeDocasCalculo.Montar([], [], Periodo(dia, dia));

        Assert.Equal(0m, r.Indicadores.TaxaReservaPct.Valor);
        Assert.Null(r.Indicadores.AproveitamentoVagaPct);
        Assert.Empty(r.HeatmapSemana);
        Assert.Empty(r.Recomendacoes);
        Assert.Equal(24, r.PorHora.Count);
    }
}
