using TruckFlow.Application;
using TruckFlow.Domain.Dto.RelatorioMotorista;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application;

public class RelatorioMotoristaServiceTests
{
    [Fact]
    public void Montar_MaisDeCincoProdutos_JuntaOResto_EmOutros()
    {
        var linhas = Enumerable.Range(1, 7)
            .Select(i => Linha(StatusAgendamento.Finalizado, new DateTime(2026, 9, i, 15, 0, 0, DateTimeKind.Utc), 1000m * i, $"Produto {i}"))
            .ToList();

        var relatorio = RelatorioMotoristaService.Montar(linhas, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        Assert.Equal(5, relatorio.PorProduto.Count);
        Assert.Equal("Produto 7", relatorio.PorProduto[0].Nome);
        var outros = relatorio.PorProduto[^1];
        Assert.Equal("Outros", outros.Nome);
        Assert.Equal(3, outros.Entregas);
        Assert.Equal(6000m, outros.PesoKg);
        Assert.Equal(100m, relatorio.PorProduto.Sum(p => p.Percentual), 0);
    }

    [Fact]
    public void Montar_NaoCompareceuEDentroDoPatio_EntramNoComparecimento()
    {
        var dia = new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc);
        var linhas = new List<RelatorioMotoristaLinha>
        {
            Linha(StatusAgendamento.Finalizado, dia, 1000m),
            Linha(StatusAgendamento.EmAndamento, dia.AddHours(1), 1000m),
            Linha(StatusAgendamento.Expirado, dia.AddHours(2), 0m),
            Linha(StatusAgendamento.Agendado, dia.AddHours(3), 0m),
        };

        var resumo = RelatorioMotoristaService.Montar(linhas, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)).Atual;

        Assert.Equal(1, resumo.Concluidas);
        Assert.Equal(1, resumo.NoPatio);
        Assert.Equal(1, resumo.Aguardando);
        Assert.Equal(66.7m, resumo.ComparecimentoPct);
        Assert.Equal(1000m, resumo.PesoKg);
    }

    private static RelatorioMotoristaLinha Linha(StatusAgendamento status, DateTime inicio, decimal peso, string produto = "Soja") =>
        new(Guid.NewGuid(), status, inicio, peso, produto, "Unidade", "Doca 1", "Fabrica");
}
