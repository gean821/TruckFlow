using TruckFlow.Application.Sefaz;

namespace TruckFlow.Test.Application;

public class ChaveAcessoHelperTests
{
    [Theory]
    [InlineData("35", "SP")]
    [InlineData("41", "PR")]
    [InlineData("43", "RS")]
    [InlineData("42", "SC")]
    [InlineData("31", "MG")]
    [InlineData("33", "RJ")]
    [InlineData("32", "ES")]
    [InlineData("29", "BA")]
    [InlineData("23", "CE")]
    [InlineData("26", "PE")]
    [InlineData("13", "AM")]
    [InlineData("51", "MT")]
    [InlineData("52", "GO")]
    [InlineData("53", "DF")]
    [InlineData("50", "MS")]
    public void ExtrairUfEmitente_CodigosValidos_RetornaUfCorreta(string prefixo, string ufEsperada)
    {
        var chave = prefixo + new string('0', 42);

        var resultado = ChaveAcessoHelper.ExtrairUfEmitente(chave);

        Assert.Equal(ufEsperada, resultado);
    }

    [Theory]
    [InlineData("99")]
    [InlineData("00")]
    [InlineData("34")]
    public void ExtrairUfEmitente_CodigoUfInexistente_RetornaNull(string prefixo)
    {
        var chave = prefixo + new string('0', 42);

        var resultado = ChaveAcessoHelper.ExtrairUfEmitente(chave);

        Assert.Null(resultado);
    }

    [Fact]
    public void ExtrairUfEmitente_ChaveComMenosDeDoisDigitos_RetornaNull()
    {
        Assert.Null(ChaveAcessoHelper.ExtrairUfEmitente("3"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtrairUfEmitente_StringVaziaOuEspacos_RetornaNull(string chave)
    {
        Assert.Null(ChaveAcessoHelper.ExtrairUfEmitente(chave));
    }

    [Fact]
    public void ExtrairUfEmitente_PrefixoNaoNumerico_RetornaNull()
    {
        Assert.Null(ChaveAcessoHelper.ExtrairUfEmitente("AB" + new string('0', 42)));
    }

    [Fact]
    public void ExtrairUfEmitente_ChaveCompletaDeSp_RetornaSp()
    {
        var chaveSpExemplo = "35" + new string('1', 42);

        Assert.Equal("SP", ChaveAcessoHelper.ExtrairUfEmitente(chaveSpExemplo));
    }
}
