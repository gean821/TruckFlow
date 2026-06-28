using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Domain;

public class RecebimentoEventoTests
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
            Nome = "Produto",
            LocalDescarga = localDescarga,
            LocalDescargaId = localDescarga.Id,
            EmpresaId = empresaId
        };
    }

    private static ItemPlanejamento CriarItem()
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
            QuantidadeTotalPlanejada = 100m,
            CadenciaDiariaPlanejada = 10m
        };
    }

    // --- Construtor ---

    [Fact]
    public void Construtor_ItemNull_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RecebimentoEvento(null!, 10m, null, null, Guid.NewGuid(), TipoMovimentoRecebimento.Reserva));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_QuantidadeInvalida_LancaArgumentException(decimal quantidade)
    {
        var item = CriarItem();
        Assert.Throws<ArgumentException>(() =>
            new RecebimentoEvento(item, quantidade, null, null, Guid.NewGuid(), TipoMovimentoRecebimento.Reserva));
    }

    [Fact]
    public void Construtor_EmpresaIdNull_LancaArgumentNullException()
    {
        var item = CriarItem();
        Assert.Throws<ArgumentNullException>(() =>
            new RecebimentoEvento(item, 10m, null, null, null, TipoMovimentoRecebimento.Reserva));
    }

    [Fact]
    public void Construtor_DadosValidos_CriaEvento()
    {
        var item = CriarItem();
        var empresaId = Guid.NewGuid();

        var evento = new RecebimentoEvento(item, 50m, null, "obs", empresaId);

        Assert.Equal(50m, evento.Quantidade);
        Assert.Equal(item.Id, evento.ItemPlanejamentoId);
        Assert.Equal(item.ProdutoId, evento.ProdutoId);
        Assert.Equal(empresaId, evento.EmpresaId);
        Assert.Equal("obs", evento.Observacao);
    }

    [Fact]
    public void Construtor_ExtaiFornecedorIdDoPlanejamento()
    {
        var item = CriarItem();
        var esperado = item.PlanejamentoRecebimento.FornecedorId;

        var evento = new RecebimentoEvento(item, 10m, null, null, Guid.NewGuid());

        Assert.Equal(esperado, evento.FornecedorId);
    }

    [Fact]
    public void Construtor_NaoEhOrfao()
    {
        var item = CriarItem();
        var evento = new RecebimentoEvento(item, 10m, null, null, Guid.NewGuid());
        Assert.False(evento.EhOrfao);
    }

    // --- CriarOrfao ---

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CriarOrfao_QuantidadeInvalida_LancaArgumentException(decimal quantidade)
    {
        Assert.Throws<ArgumentException>(() =>
            RecebimentoEvento.CriarOrfao(Guid.NewGuid(), null, quantidade, null, null, Guid.NewGuid()));
    }

    [Fact]
    public void CriarOrfao_DadosValidos_CriaEventoOrfao()
    {
        var produtoId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();

        var orfao = RecebimentoEvento.CriarOrfao(produtoId, null, 30m, null, "obs", empresaId);

        Assert.Equal(produtoId, orfao.ProdutoId);
        Assert.Equal(30m, orfao.Quantidade);
        Assert.Equal("obs", orfao.Observacao);
        Assert.True(orfao.EhOrfao);
        Assert.Null(orfao.ItemPlanejamentoId);
    }

    [Fact]
    public void CriarOrfao_EhOrfao_RetornaTrue()
    {
        var orfao = RecebimentoEvento.CriarOrfao(Guid.NewGuid(), null, 10m, null, null, Guid.NewGuid());
        Assert.True(orfao.EhOrfao);
    }

    // --- Vincular ---

    [Fact]
    public void Vincular_ItemNull_LancaArgumentNullException()
    {
        var orfao = RecebimentoEvento.CriarOrfao(Guid.NewGuid(), null, 10m, null, null, Guid.NewGuid());
        Assert.Throws<ArgumentNullException>(() => orfao.Vincular(null!));
    }

    [Fact]
    public void Vincular_EventoJaVinculado_LancaInvalidOperationException()
    {
        var item = CriarItem();
        var evento = new RecebimentoEvento(item, 10m, null, null, Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => evento.Vincular(item));
    }

    [Fact]
    public void Vincular_OrfaoValido_VinculaItem()
    {
        var item = CriarItem();
        var orfao = RecebimentoEvento.CriarOrfao(null, null, 10m, null, null, Guid.NewGuid());

        orfao.Vincular(item);

        Assert.Equal(item.Id, orfao.ItemPlanejamentoId);
        Assert.Equal(item, orfao.ItemPlanejamento);
        Assert.Equal(item.ProdutoId, orfao.ProdutoId);
    }

    [Fact]
    public void Vincular_OrfaoValido_NaoEhMaisOrfao()
    {
        var item = CriarItem();
        var orfao = RecebimentoEvento.CriarOrfao(null, null, 10m, null, null, Guid.NewGuid());

        orfao.Vincular(item);

        Assert.False(orfao.EhOrfao);
    }
}
