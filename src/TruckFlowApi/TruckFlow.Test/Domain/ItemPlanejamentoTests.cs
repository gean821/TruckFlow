using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Domain;

public class ItemPlanejamentoTests
{
    private static Produto CriarProduto(Guid empresaId)
    {
        var unidade = new UnidadeEntrega { Nome = "Unidade", Localizacao = "Pátio A", EmpresaId = empresaId };
        var localDescarga = new LocalDescarga
        {
            Nome = "LD Teste",
            UnidadeEntrega = unidade,
            UnidadeEntregaId = unidade.Id,
            EmpresaId = empresaId
        };
        return new Produto
        {
            Nome = "Milho",
            LocalDescarga = localDescarga,
            LocalDescargaId = localDescarga.Id,
            EmpresaId = empresaId
        };
    }

    private static ItemPlanejamento CriarItem(
        decimal planejada = 100m,
        decimal cadencia = 20m,
        decimal tolerancia = 30m,
        string diasSemana = "0,1,2,3,4,5,6") // todos os dias
    {
        var empresaId = Guid.NewGuid();
        var produto = CriarProduto(empresaId);
        var fornecedor = new Fornecedor { Nome = "Forn.", Cnpj = "00000000000000", EmpresaId = empresaId };
        var planejamento = new PlanejamentoRecebimento
        {
            Fornecedor = fornecedor,
            FornecedorId = fornecedor.Id,
            DataInicio = DateTime.Today,
            DataFim = DateTime.Today.AddDays(30),
            EmpresaId = empresaId
        };

        return new ItemPlanejamento
        {
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = planejamento,
            PlanejamentoRecebimentoId = planejamento.Id,
            QuantidadeTotalPlanejada = planejada,
            CadenciaDiariaPlanejada = cadencia,
            ToleranciaExtra = tolerancia,
            DiasSemana = diasSemana
        };
    }

    // --- Reservar ---

    [Fact]
    public void Reservar_QuantidadeValida_IncrementaReservada()
    {
        var item = CriarItem();
        item.Reservar(50m);
        Assert.Equal(50m, item.QuantidadeReservada);
    }

    [Fact]
    public void Reservar_Acumulado_SomaCadaReserva()
    {
        var item = CriarItem();
        item.Reservar(30m);
        item.Reservar(20m);
        Assert.Equal(50m, item.QuantidadeReservada);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reservar_QuantidadeInvalida_LancaExcecao(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<Exception>(() => item.Reservar(quantidade));
    }

    // --- EstornarReserva ---

    [Fact]
    public void EstornarReserva_QuantidadeValida_DecrementaReservada()
    {
        var item = CriarItem();
        item.Reservar(60m);
        item.EstornarReserva(40m);
        Assert.Equal(20m, item.QuantidadeReservada);
    }

    [Fact]
    public void EstornarReserva_QuantidadeMaiorQueReservada_NaoFicaNegativa()
    {
        var item = CriarItem();
        item.Reservar(10m);
        item.EstornarReserva(50m);
        Assert.Equal(0m, item.QuantidadeReservada);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void EstornarReserva_QuantidadeInvalida_LancaExcecao(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<Exception>(() => item.EstornarReserva(quantidade));
    }

    // --- ConfirmarRecebimento ---

    [Fact]
    public void ConfirmarRecebimento_SemReservaOriginal_IncrementaTotalRecebido()
    {
        var item = CriarItem();
        item.ConfirmarRecebimento(80m, 0m);
        Assert.Equal(80m, item.QuantidadeTotalRecebida);
        Assert.Equal(0m, item.QuantidadeReservada);
    }

    [Fact]
    public void ConfirmarRecebimento_ComReservaOriginal_DecrementaReservaEIncrementaRecebido()
    {
        var item = CriarItem();
        item.Reservar(50m);
        item.ConfirmarRecebimento(45m, 50m);
        Assert.Equal(45m, item.QuantidadeTotalRecebida);
        Assert.Equal(0m, item.QuantidadeReservada);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConfirmarRecebimento_QuantidadeInvalida_LancaExcecao(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<Exception>(() => item.ConfirmarRecebimento(quantidade, 0));
    }

    // --- RegistrarRecebimento ---

    [Fact]
    public void RegistrarRecebimento_QuantidadeValida_IncrementaTotalRecebido()
    {
        var item = CriarItem();
        item.RegistrarRecebimento(25m);
        Assert.Equal(25m, item.QuantidadeTotalRecebida);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void RegistrarRecebimento_QuantidadeInvalida_LancaExcecao(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<Exception>(() => item.RegistrarRecebimento(quantidade));
    }

    // --- EstornarRecebimento ---

    [Fact]
    public void EstornarRecebimento_QuantidadeValida_DecrementaTotalRecebido()
    {
        var item = CriarItem();
        item.RegistrarRecebimento(80m);
        item.EstornarRecebimento(30m);
        Assert.Equal(50m, item.QuantidadeTotalRecebida);
    }

    [Fact]
    public void EstornarRecebimento_QuantidadeMaiorQueRecebido_NaoFicaNegativa()
    {
        var item = CriarItem();
        item.RegistrarRecebimento(10m);
        item.EstornarRecebimento(100m);
        Assert.Equal(0m, item.QuantidadeTotalRecebida);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EstornarRecebimento_QuantidadeInvalida_LancaExcecao(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<Exception>(() => item.EstornarRecebimento(quantidade));
    }

    // --- EstaConcluido ---

    [Fact]
    public void EstaConcluido_TotalRecebidoAtingePlano_RetornaTrue()
    {
        var item = CriarItem(planejada: 100m);
        item.RegistrarRecebimento(100m);
        Assert.True(item.EstaConcluido());
    }

    [Fact]
    public void EstaConcluido_ReservaAtingePlano_RetornaTrue()
    {
        var item = CriarItem(planejada: 100m);
        item.Reservar(100m);
        Assert.True(item.EstaConcluido());
    }

    [Fact]
    public void EstaConcluido_SomaReservaRecebidoAtingePlano_RetornaTrue()
    {
        var item = CriarItem(planejada: 100m);
        item.Reservar(40m);
        item.RegistrarRecebimento(60m);
        Assert.True(item.EstaConcluido());
    }

    [Fact]
    public void EstaConcluido_QuantidadeAbaixoDoPlano_RetornaFalse()
    {
        var item = CriarItem(planejada: 100m);
        item.RegistrarRecebimento(99m);
        Assert.False(item.EstaConcluido());
    }

    // --- SaldoDisponivel ---

    [Fact]
    public void SaldoDisponivel_SemMovimentacao_RetornaPlanejadaMaisTolerancia()
    {
        var item = CriarItem(planejada: 100m, tolerancia: 30m);
        Assert.Equal(130m, item.SaldoDisponivel);
    }

    [Fact]
    public void SaldoDisponivel_ComReservaERecebimento_DecrementaCorretamente()
    {
        var item = CriarItem(planejada: 100m, tolerancia: 30m);
        item.Reservar(50m);
        item.RegistrarRecebimento(20m);
        Assert.Equal(60m, item.SaldoDisponivel);
    }

    [Fact]
    public void SaldoDisponivel_ExcessoMovimentacao_NaoFicaNegativo()
    {
        var item = CriarItem(planejada: 10m, tolerancia: 0m);
        item.RegistrarRecebimento(10m);
        item.Reservar(5m);
        Assert.Equal(0m, item.SaldoDisponivel);
    }

    // --- OperaEm / DiasSemanaEnum ---

    [Fact]
    public void OperaEm_DiaNaLista_RetornaTrue()
    {
        var item = CriarItem(diasSemana: "1,2,3"); // seg, ter, qua
        var segunda = ObterProximoDia(DayOfWeek.Monday);
        Assert.True(item.OperaEm(segunda));
    }

    [Fact]
    public void OperaEm_DiaNaoNaLista_RetornaFalse()
    {
        var item = CriarItem(diasSemana: "1,2,3"); // seg, ter, qua
        var sabado = ObterProximoDia(DayOfWeek.Saturday);
        Assert.False(item.OperaEm(sabado));
    }

    [Fact]
    public void DiasSemanaEnum_StringVazia_RetornaListaVazia()
    {
        var item = CriarItem(diasSemana: "");
        Assert.Empty(item.DiasSemanaEnum);
    }

    [Fact]
    public void DiasSemanaEnum_DiasEspecificos_RetornaDiasCorretos()
    {
        var item = CriarItem(diasSemana: "1,3,5");
        var dias = item.DiasSemanaEnum;
        Assert.Contains(DayOfWeek.Monday, dias);
        Assert.Contains(DayOfWeek.Wednesday, dias);
        Assert.Contains(DayOfWeek.Friday, dias);
        Assert.Equal(3, dias.Count);
    }

    // --- QuantidadeRecebidaNoDia / MetaDiariaAtingida ---

    [Fact]
    public void QuantidadeRecebidaNoDia_SomaEventosNoDia()
    {
        var item = CriarItem();
        var hoje = DateTime.Today;
        item.RecebimentoEventos = new List<RecebimentoEvento>
        {
            CriarEvento(item, 30m, hoje),
            CriarEvento(item, 20m, hoje),
            CriarEvento(item, 10m, hoje.AddDays(-1))
        };
        Assert.Equal(50m, item.QuantidadeRecebidaNoDia(hoje));
    }

    [Fact]
    public void MetaDiariaAtingida_NaoOperaNoDia_RetornaFalse()
    {
        var sabado = ObterProximoDia(DayOfWeek.Saturday);
        var item = CriarItem(cadencia: 20m, tolerancia: 0m, diasSemana: "1,2,3,4,5"); // seg-sex
        item.RecebimentoEventos = new List<RecebimentoEvento>
        {
            CriarEvento(item, 999m, sabado)
        };
        Assert.False(item.MetaDiariaAtingida(sabado));
    }

    [Fact]
    public void MetaDiariaAtingida_RecebimentoAtingeMaisCadenciaMaisTolerancia_RetornaTrue()
    {
        var hoje = DateTime.Today;
        var item = CriarItem(cadencia: 20m, tolerancia: 5m, diasSemana: $"{(int)hoje.DayOfWeek}");
        item.RecebimentoEventos = new List<RecebimentoEvento>
        {
            CriarEvento(item, 25m, hoje) // 25 >= 20+5
        };
        Assert.True(item.MetaDiariaAtingida(hoje));
    }

    [Fact]
    public void MetaDiariaAtingida_RecebimentoAbaixoDaMeta_RetornaFalse()
    {
        var hoje = DateTime.Today;
        var item = CriarItem(cadencia: 20m, tolerancia: 5m, diasSemana: $"{(int)hoje.DayOfWeek}");
        item.RecebimentoEventos = new List<RecebimentoEvento>
        {
            CriarEvento(item, 24m, hoje) // 24 < 25
        };
        Assert.False(item.MetaDiariaAtingida(hoje));
    }

    private static DateTime ObterProximoDia(DayOfWeek dia)
    {
        var hoje = DateTime.Today;
        var diff = ((int)dia - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diff == 0 ? 7 : diff);
    }

    private static RecebimentoEvento CriarEvento(ItemPlanejamento item, decimal quantidade, DateTime data)
    {
        var evento = RecebimentoEvento.CriarOrfao(item.ProdutoId, null, quantidade, null, null, Guid.NewGuid());
        typeof(RecebimentoEvento)
            .GetProperty("DataRecebimento")!
            .SetValue(evento, data);
        return evento;
    }
}
