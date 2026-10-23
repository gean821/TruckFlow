using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Domain;

public class PlanejamentoRecebimentoTests
{
    private static Fornecedor CriarFornecedor(Guid? empresaId = null) => new()
    {
        Nome = "Forn. Teste",
        Cnpj = "00000000000000",
        EmpresaId = empresaId ?? Guid.NewGuid()
    };

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
            Nome = "Produto",
            LocalDescarga = localDescarga,
            LocalDescargaId = localDescarga.Id,
            EmpresaId = empresaId
        };
    }

    private static PlanejamentoRecebimento CriarPlanejamento(
        DateTime? inicio = null,
        DateTime? fim = null)
    {
        var forn = CriarFornecedor();
        return new PlanejamentoRecebimento
        {
            Fornecedor = forn,
            FornecedorId = forn.Id,
            DataInicio = inicio ?? DateTime.Today,
            DataFim = fim ?? DateTime.Today.AddDays(30),
            EmpresaId = Guid.NewGuid()
        };
    }

    private static ItemPlanejamento CriarItemConcluido(PlanejamentoRecebimento planejamento, decimal planejada = 100m)
    {
        var produto = CriarProduto(planejamento.EmpresaId);
        var item = new ItemPlanejamento
        {
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = planejamento,
            PlanejamentoRecebimentoId = planejamento.Id,
            QuantidadeTotalPlanejada = planejada,
            CadenciaDiariaPlanejada = 10m,
            QuantidadeTotalRecebida = planejada
        };
        return item;
    }

    private static ItemPlanejamento CriarItemEmAndamento(PlanejamentoRecebimento planejamento)
    {
        var produto = CriarProduto(planejamento.EmpresaId);
        return new ItemPlanejamento
        {
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = planejamento,
            PlanejamentoRecebimentoId = planejamento.Id,
            QuantidadeTotalPlanejada = 100m,
            CadenciaDiariaPlanejada = 10m,
            QuantidadeTotalRecebida = 50m
        };
    }

    private static ItemPlanejamento CriarItemPlanejado(PlanejamentoRecebimento planejamento)
    {
        var produto = CriarProduto(planejamento.EmpresaId);
        return new ItemPlanejamento
        {
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = planejamento,
            PlanejamentoRecebimentoId = planejamento.Id,
            QuantidadeTotalPlanejada = 100m,
            CadenciaDiariaPlanejada = 10m,
            QuantidadeTotalRecebida = 0m
        };
    }

    // --- RecalcularStatus ---

    [Fact]
    public void RecalcularStatus_Encerrado_NaoAlteraStatus()
    {
        var p = CriarPlanejamento();
        p.StatusRecebimento = StatusRecebimento.Encerrado;
        p.ItemPlanejamentos.Add(CriarItemConcluido(p));

        p.RecalcularStatus();

        Assert.Equal(StatusRecebimento.Encerrado, p.StatusRecebimento);
    }

    [Fact]
    public void RecalcularStatus_TodosItensCompletos_DefineConcluido()
    {
        var p = CriarPlanejamento();
        p.ItemPlanejamentos.Add(CriarItemConcluido(p));
        p.ItemPlanejamentos.Add(CriarItemConcluido(p));

        p.RecalcularStatus();

        Assert.Equal(StatusRecebimento.Concluido, p.StatusRecebimento);
    }

    [Fact]
    public void RecalcularStatus_AlgumItemComRecebimento_DefineEmAndamento()
    {
        var p = CriarPlanejamento();
        p.ItemPlanejamentos.Add(CriarItemEmAndamento(p));
        p.ItemPlanejamentos.Add(CriarItemPlanejado(p));

        p.RecalcularStatus();

        Assert.Equal(StatusRecebimento.EmAndamento, p.StatusRecebimento);
    }

    [Fact]
    public void RecalcularStatus_AlgumItemComReserva_DefineEmAndamento()
    {
        var p = CriarPlanejamento();
        var item = CriarItemPlanejado(p);
        item.Reservar(10m);
        p.ItemPlanejamentos.Add(item);
        p.ItemPlanejamentos.Add(CriarItemPlanejado(p));

        p.RecalcularStatus();

        Assert.Equal(StatusRecebimento.EmAndamento, p.StatusRecebimento);
    }

    [Fact]
    public void RecalcularStatus_NenhumItemIniciado_DefinePlanejado()
    {
        var p = CriarPlanejamento();
        p.ItemPlanejamentos.Add(CriarItemPlanejado(p));
        p.ItemPlanejamentos.Add(CriarItemPlanejado(p));

        p.RecalcularStatus();

        Assert.Equal(StatusRecebimento.Planejado, p.StatusRecebimento);
    }

    // --- VigenciaContem ---

    [Fact]
    public void VigenciaContem_DataDentroDoIntervalo_RetornaTrue()
    {
        var p = CriarPlanejamento(DateTime.Today, DateTime.Today.AddDays(10));
        Assert.True(p.VigenciaContem(DateTime.Today.AddDays(5)));
    }

    [Fact]
    public void VigenciaContem_DataExataInicio_RetornaTrue()
    {
        var p = CriarPlanejamento(DateTime.Today, DateTime.Today.AddDays(10));
        Assert.True(p.VigenciaContem(DateTime.Today));
    }

    [Fact]
    public void VigenciaContem_DataExataFim_RetornaTrue()
    {
        var fim = DateTime.Today.AddDays(10);
        var p = CriarPlanejamento(DateTime.Today, fim);
        Assert.True(p.VigenciaContem(fim));
    }

    [Fact]
    public void VigenciaContem_DataAnteriorAoInicio_RetornaFalse()
    {
        var p = CriarPlanejamento(DateTime.Today, DateTime.Today.AddDays(10));
        Assert.False(p.VigenciaContem(DateTime.Today.AddDays(-1)));
    }

    [Fact]
    public void VigenciaContem_DataAposOfim_RetornaFalse()
    {
        var p = CriarPlanejamento(DateTime.Today, DateTime.Today.AddDays(10));
        Assert.False(p.VigenciaContem(DateTime.Today.AddDays(11)));
    }

    // --- ItemDoProduto ---

    [Fact]
    public void ItemDoProduto_ProdutoExistente_RetornaItem()
    {
        var p = CriarPlanejamento();
        var item = CriarItemPlanejado(p);
        p.ItemPlanejamentos.Add(item);

        var resultado = p.ItemDoProduto(item.ProdutoId);

        Assert.NotNull(resultado);
        Assert.Equal(item.ProdutoId, resultado.ProdutoId);
    }

    [Fact]
    public void ItemDoProduto_ProdutoInexistente_RetornaNull()
    {
        var p = CriarPlanejamento();
        p.ItemPlanejamentos.Add(CriarItemPlanejado(p));

        var resultado = p.ItemDoProduto(Guid.NewGuid());

        Assert.Null(resultado);
    }

    // --- DeveCongelarProduto ---

    [Fact]
    public void DeveCongelarProduto_ProdutoNaoExiste_RetornaFalse()
    {
        var p = CriarPlanejamento();
        Assert.False(p.DeveCongelarProduto(Guid.NewGuid(), DateTime.Today));
    }

    [Fact]
    public void DeveCongelarProduto_MetaNaoAtingida_RetornaFalse()
    {
        var hoje = DateTime.Today;
        var p = CriarPlanejamento();
        var produto = CriarProduto(p.EmpresaId);
        var item = new ItemPlanejamento
        {
            Produto = produto,
            ProdutoId = produto.Id,
            PlanejamentoRecebimento = p,
            PlanejamentoRecebimentoId = p.Id,
            QuantidadeTotalPlanejada = 100m,
            CadenciaDiariaPlanejada = 20m,
            ToleranciaExtra = 5m,
            DiasSemana = $"{(int)hoje.DayOfWeek}",
            RecebimentoEventos = new List<RecebimentoEvento>
            {
                RecebimentoEvento.CriarOrfao(produto.Id, null, 10m, null, null, p.EmpresaId)
            }
        };
        p.ItemPlanejamentos.Add(item);

        Assert.False(p.DeveCongelarProduto(produto.Id, hoje));
    }
}
