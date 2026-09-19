using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TruckFlow.Application.NotaFiscais;
using TruckFlow.Application.Sefaz;
using TruckFlow.Test.Application.NotaFiscal;

namespace TruckFlow.Test.Application.Sefaz;

public class FakeSefazClientDistribuicaoTests
{
    private static FakeSefazClient CriarCliente()
    {
        var options = Options.Create(new SefazOptions { Ambiente = 2 });
        return new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, options);
    }

    [Fact]
    public async Task ConsultarDistribuicaoAsync_ChavePadrao_RetornaAutorizadaComXmlDisponivel()
    {
        var cliente = CriarCliente();

        var resultado = await cliente.ConsultarDistribuicaoAsync("35240199999999999999550010000099999999999999", CancellationToken.None);

        Assert.True(resultado.Disponivel);
        Assert.Equal(138, resultado.CStat);
        Assert.NotNull(resultado.XmlNfe);
    }

    [Fact]
    public async Task ConsultarDistribuicaoAsync_ChavePadrao_XmlRetornadoEhParseavelPeloExtractor()
    {
        var cliente = CriarCliente();

        var resultado = await cliente.ConsultarDistribuicaoAsync("35240199999999999999550010000099999999999999", CancellationToken.None);

        var nfe = NotaFiscalXmlExtractor.ParseXml(resultado.XmlNfe!);
        var dto = NotaFiscalXmlExtractor.Extrair(nfe);

        Assert.Equal("Fornecedor Teste LTDA", dto.EmitenteNome);
        Assert.Equal(1000.000m, dto.PesoBruto);
    }

    [Fact]
    public async Task ConsultarDistribuicaoAsync_ChaveTerminadaEm0010_RetornaNaoDisponivel()
    {
        var cliente = CriarCliente();

        var resultado = await cliente.ConsultarDistribuicaoAsync(NotaFiscalXmlFixtures.ChaveNaoDisponivel, CancellationToken.None);

        Assert.False(resultado.Disponivel);
        Assert.Null(resultado.XmlNfe);
        Assert.Equal(137, resultado.CStat);
    }

    [Fact]
    public async Task ConsultarDistribuicaoAsync_ChaveTerminadaEm0020_RetornaDenegadaSemXml()
    {
        var cliente = CriarCliente();

        var resultado = await cliente.ConsultarDistribuicaoAsync(NotaFiscalXmlFixtures.ChaveDenegada, CancellationToken.None);

        Assert.False(resultado.Disponivel);
        Assert.Equal(110, resultado.CStat);
    }

    [Fact]
    public async Task ConsultarDistribuicaoAsync_ResultadoTrazAChaveAcessoRecebida()
    {
        var cliente = CriarCliente();
        const string chave = "35240199999999999999550010000099999999999999";

        var resultado = await cliente.ConsultarDistribuicaoAsync(chave, CancellationToken.None);

        Assert.Equal(chave, resultado.ChaveAcesso);
    }
}
