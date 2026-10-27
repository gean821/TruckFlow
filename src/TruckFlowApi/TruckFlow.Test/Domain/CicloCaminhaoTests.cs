using TruckFlow.Domain.Rules;

namespace TruckFlow.Test.Domain;

public class CicloCaminhaoTests
{
    private static readonly Guid Doca1 = Guid.NewGuid();
    private static readonly Guid Doca2 = Guid.NewGuid();
    private static readonly DateTime Dia = new(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime H(int hora, int minuto = 0) => Dia.AddHours(hora).AddMinutes(minuto);

    [Fact]
    public void Calcular_FilaNaDoca_InicioEstimadoEhOFimDoCaminhaoAnterior()
    {
        var a = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, H(8), null, H(9), null);
        var b = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, H(8, 10), null, H(10), H(10, 20));

        var resultado = CicloCaminhao.Calcular([a, b]).ToDictionary(r => r.AgendamentoId);

        var primeiro = resultado[a.AgendamentoId];
        Assert.Equal(H(8), primeiro.InicioDescarga);
        Assert.True(primeiro.InicioEstimado);
        Assert.Equal(0, primeiro.EsperaMinutos);
        Assert.Equal(60, primeiro.DescargaMinutos);
        Assert.True(primeiro.SaidaEstimada);
        Assert.Equal(60, primeiro.PermanenciaMinutos);

        var segundo = resultado[b.AgendamentoId];
        Assert.Equal(H(9), segundo.InicioDescarga);
        Assert.Equal(50, segundo.EsperaMinutos);
        Assert.Equal(60, segundo.DescargaMinutos);
        Assert.False(segundo.SaidaEstimada);
        Assert.Equal(130, segundo.PermanenciaMinutos);
    }

    [Fact]
    public void Calcular_OutraDocaNaoInterfere_EHorarioRealVenceEstimativa()
    {
        var naDoca1 = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, H(7), null, H(11), null);
        var naDoca2 = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca2, H(8), null, H(9), null);
        var comInicioReal = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, H(10), H(11, 15), H(12), null);

        var resultado = CicloCaminhao.Calcular([naDoca1, naDoca2, comInicioReal]).ToDictionary(r => r.AgendamentoId);

        Assert.Equal(H(8), resultado[naDoca2.AgendamentoId].InicioDescarga);
        Assert.Equal(0, resultado[naDoca2.AgendamentoId].EsperaMinutos);

        var real = resultado[comInicioReal.AgendamentoId];
        Assert.Equal(H(11, 15), real.InicioDescarga);
        Assert.False(real.InicioEstimado);
        Assert.Equal(75, real.EsperaMinutos);
        Assert.Equal(45, real.DescargaMinutos);
    }

    [Fact]
    public void Calcular_SemChegadaOuSemFim_NaoInventaTempo()
    {
        var semChegada = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, null, null, H(9), null);
        var noPatio = new CicloCaminhaoEntrada(Guid.NewGuid(), Doca1, H(8), null, null, null);

        var resultado = CicloCaminhao.Calcular([semChegada, noPatio]).ToDictionary(r => r.AgendamentoId);

        Assert.Null(resultado[semChegada.AgendamentoId].InicioDescarga);
        Assert.Null(resultado[semChegada.AgendamentoId].EsperaMinutos);
        Assert.Null(resultado[semChegada.AgendamentoId].PermanenciaMinutos);

        Assert.Null(resultado[noPatio.AgendamentoId].InicioDescarga);
        Assert.Null(resultado[noPatio.AgendamentoId].Saida);
        Assert.Null(resultado[noPatio.AgendamentoId].DescargaMinutos);
    }
}
