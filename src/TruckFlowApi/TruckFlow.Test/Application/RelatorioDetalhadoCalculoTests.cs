using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class RelatorioDetalhadoCalculoTests
{
    private static readonly DateTime Inicio = new(2026, 10, 20, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fim = Inicio.AddHours(1);
    private static readonly Guid Doca = Guid.NewGuid();

    [Theory]
    [InlineData(StatusAgendamento.Finalizado, -5, SituacaoChegada.Adiantado)]
    [InlineData(StatusAgendamento.Finalizado, 0, SituacaoChegada.NoHorario)]
    [InlineData(StatusAgendamento.EmAndamento, 60, SituacaoChegada.NoHorario)]
    [InlineData(StatusAgendamento.EmAndamento, 61, SituacaoChegada.Atrasado)]
    public void Situacao_ComparaAChegadaComAJanela(StatusAgendamento status, int minutos, SituacaoChegada esperada)
    {
        Assert.Equal(esperada, RelatorioDetalhadoCalculo.Situacao(status, Inicio, Fim, Inicio.AddMinutes(minutos)));
    }

    [Theory]
    [InlineData(StatusAgendamento.Expirado, SituacaoChegada.NaoCompareceu)]
    [InlineData(StatusAgendamento.Agendado, null)]
    [InlineData(StatusAgendamento.Cancelado, null)]
    public void Situacao_SemChegada_SoExpiradoEhNaoComparecimento(StatusAgendamento status, SituacaoChegada? esperada)
    {
        Assert.Equal(esperada, RelatorioDetalhadoCalculo.Situacao(status, Inicio, Fim, null));
    }

    [Fact]
    public void Percentil_UsaPostoMaisProximo()
    {
        var valores = Enumerable.Range(1, 10).Select(v => (double)v).ToList();

        Assert.Equal(9d, RelatorioDetalhadoCalculo.Percentil(valores, 0.9));
        Assert.Equal(10d, RelatorioDetalhadoCalculo.Percentil([10d], 0.9));
        Assert.Null(RelatorioDetalhadoCalculo.Percentil([], 0.9));
    }

    private static RelatorioResumoLinha Linha(StatusAgendamento status, int? chegouMin = null, int? fimMin = null, decimal peso = 1000m) =>
        new(Guid.NewGuid(), status, Inicio, Fim, Doca,
            chegouMin.HasValue ? Inicio.AddMinutes(chegouMin.Value) : null,
            null,
            fimMin.HasValue ? Inicio.AddMinutes(fimMin.Value) : null,
            null,
            peso);

    [Fact]
    public void Resumo_ContaStatusPontualidadeETemposDoCiclo()
    {
        var linhas = new List<RelatorioResumoLinha>
        {
            Linha(StatusAgendamento.Finalizado, chegouMin: -10, fimMin: 30),
            Linha(StatusAgendamento.Finalizado, chegouMin: 10, fimMin: 70),
            Linha(StatusAgendamento.EmAndamento, chegouMin: 90),
            Linha(StatusAgendamento.Expirado),
            Linha(StatusAgendamento.Agendado, peso: 500m),
        };

        var fins = RelatorioDetalhadoCalculo.FinsPorDoca(
            linhas.Where(l => l.FimDescargaEm.HasValue).Select(l => new FimDescargaDoca(Doca, l.FimDescargaEm!.Value)));

        var r = RelatorioDetalhadoCalculo.Resumo(linhas, fins);

        Assert.Equal(5, r.Total);
        Assert.Equal(4500m, r.PesoTotalKg);
        Assert.Equal(["Agendado", "EmAndamento", "Finalizado", "Expirado"], r.PorStatus.Select(s => s.Status));
        Assert.Equal(2, r.PorStatus.Single(s => s.Status == "Finalizado").Quantidade);
        Assert.Equal("Não compareceu", r.PorStatus.Single(s => s.Status == "Expirado").Rotulo);

        Assert.Equal(3, r.Chegaram);
        Assert.Equal(1, r.Adiantados);
        Assert.Equal(1, r.NoHorario);
        Assert.Equal(1, r.Atrasados);
        Assert.Equal(1, r.NaoCompareceram);
        Assert.Equal(66.7m, r.PontualidadePct);
        Assert.Equal(90d, r.AtrasoMedioMin);

        Assert.Equal(10d, r.EsperaMediaMin);
        Assert.Equal(20d, r.EsperaP90Min);
        Assert.Equal(40d, r.DescargaMediaMin);
        Assert.Equal(50d, r.PermanenciaMediaMin);
        Assert.True(r.TemposEstimados);
    }

    [Fact]
    public void Resumo_Vazio_NaoDividePorZero()
    {
        var r = RelatorioDetalhadoCalculo.Resumo([], RelatorioDetalhadoCalculo.FinsPorDoca([]));

        Assert.Equal(0, r.Total);
        Assert.Null(r.PontualidadePct);
        Assert.Null(r.EsperaMediaMin);
        Assert.Empty(r.PorStatus);
        Assert.False(r.TemposEstimados);
    }

    [Fact]
    public void Mapear_TraduzStatusOrigemEAtraso()
    {
        var dto = RelatorioDetalhadoCalculo.Mapear(new AgendamentoRelatorioLinha
        {
            Id = Guid.NewGuid(),
            DaGrade = false,
            TipoCarga = TipoCarga.Milho,
            TipoVeiculo = TipoVeiculo.BiTrem,
            Status = StatusAgendamento.Expirado,
            DataInicio = Inicio,
            DataFim = Fim,
        });

        Assert.Equal("Expirado", dto.Status);
        Assert.Equal("Não compareceu", dto.StatusRotulo);
        Assert.Equal("Avulso", dto.Origem);
        Assert.Equal("Milho", dto.Produto);
        Assert.Equal("Bitrem", dto.TipoVeiculo);
        Assert.Equal("NaoCompareceu", dto.SituacaoChegada);
        Assert.Null(dto.AtrasoChegadaMin);
        Assert.False(dto.InicioEstimado);
    }

    [Theory]
    [InlineData(TipoVeiculo.CarretaDoisEixos, "Carreta 2 eixos")]
    [InlineData(TipoVeiculo.Ls, "LS (cavalo mecânico)")]
    [InlineData(TipoVeiculo.CarretaCavaloTrucado, "Carreta cavalo trucado")]
    [InlineData(TipoVeiculo.BiTrem, "Bitrem")]
    [InlineData(TipoVeiculo.TriTrem, "Tritrem")]
    [InlineData(TipoVeiculo.RodoTrem, "Rodotrem")]
    [InlineData(TipoVeiculo.CarretaBau, "Carreta baú")]
    [InlineData(TipoVeiculo.CarretaSider, "Carreta sider")]
    [InlineData(TipoVeiculo.Vanderleia, "Vanderléia")]
    [InlineData(TipoVeiculo.BauFrigorifico, "Baú frigorífico")]
    public void TipoVeiculoRotulo_TraduzTodosOsTipos(TipoVeiculo tipo, string esperado)
    {
        Assert.Equal(esperado, RelatorioDetalhadoCalculo.TipoVeiculoRotulo(tipo));
    }

    [Fact]
    public void TipoVeiculoRotulo_SemTipo_DevolveNulo()
    {
        Assert.Null(RelatorioDetalhadoCalculo.TipoVeiculoRotulo(null));
    }
}
