using TruckFlow.Application.Exceptions;
using TruckFlow.Application.NotaFiscais;

namespace TruckFlow.Test.Application.NotaFiscal;

public class NotaFiscalXmlExtractorTests
{
    [Fact]
    public void ParseXml_NfeProc_RetornaNfeValida()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeProcAutorizada);

        Assert.NotNull(nfe);
        Assert.NotNull(nfe.infNFe);
    }

    [Fact]
    public void ParseXml_NfeBare_RetornaNfeValida()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeBareAutorizada);

        Assert.NotNull(nfe);
        Assert.NotNull(nfe.infNFe);
    }

    [Fact]
    public void ParseXml_XmlInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(
            () => NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.XmlInvalido));
    }

    [Fact]
    public void ParseXml_XmlVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(
            () => NotaFiscalXmlExtractor.ParseXml(""));
    }

    [Fact]
    public void Extrair_NotaAutorizadaCompleta_ExtraiTodosOsCamposCorretamente()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeProcAutorizada);

        var dto = NotaFiscalXmlExtractor.Extrair(nfe);

        Assert.Equal(NotaFiscalXmlFixtures.ChaveAutorizada, dto.ChaveAcesso);
        Assert.Equal(12345, dto.Numero);
        Assert.Equal("1", dto.Serie);
        Assert.Equal("Fornecedor Teste LTDA", dto.EmitenteNome);
        Assert.Equal("11222333000181", dto.EmitenteCnpj);
        Assert.Equal("Aurora Alimentos Teste", dto.DestinatarioNome);
        Assert.Equal("99888777000166", dto.DestinatarioCpfCnpj);
        Assert.Equal(1500.00m, dto.ValorTotal);
        Assert.Equal(1000.000m, dto.PesoBruto);
        Assert.Equal(20, dto.VolumeQuantidade);
        Assert.Equal("ABC1D23", dto.PlacaVeiculo);
        Assert.Single(dto.Itens);
    }

    [Fact]
    public void Extrair_NotaAutorizadaCompleta_ExtraiItemCorretamente()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeProcAutorizada);

        var dto = NotaFiscalXmlExtractor.Extrair(nfe);
        var item = dto.Itens[0];

        Assert.Equal("PROD001", item.Codigo);
        Assert.Equal("7891000100103", item.Ean);
        Assert.Equal("Milho em Grao", item.Descricao);
        Assert.Equal(1000.0000m, item.Quantidade);
        Assert.Equal("KG", item.Unidade);
        Assert.Equal(1.5000000000m, item.ValorUnitario);
        Assert.Equal(1500.00m, item.ValorTotal);
    }

    [Fact]
    public void Extrair_NotaSemBlocoTransporte_PesoEPlacaFicamNulos()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeBareAutorizada);

        var dto = NotaFiscalXmlExtractor.Extrair(nfe);

        Assert.Null(dto.PesoBruto);
        Assert.Null(dto.VolumeQuantidade);
        Assert.Equal(string.Empty, dto.PlacaVeiculo);
    }

    [Fact]
    public void Extrair_ChaveAcesso_VemDoAtributoIdSemPrefixoNFe()
    {
        var nfe = NotaFiscalXmlExtractor.ParseXml(NotaFiscalXmlFixtures.NfeProcAutorizada);

        var dto = NotaFiscalXmlExtractor.Extrair(nfe);

        Assert.DoesNotContain("NFe", dto.ChaveAcesso);
        Assert.Equal(44, dto.ChaveAcesso.Length);
    }
}
