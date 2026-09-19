using TruckFlow.Domain.Entities;

namespace TruckFlow.Test.Domain;

public class EmpresaAuthMethodsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ConfiguracoesVazio_RetornaSoLocal(string? configuracoes)
    {
        var resultado = EmpresaAuthMethods.Resolve(configuracoes);

        Assert.Single(resultado, EmpresaAuthMethods.Local);
    }

    [Fact]
    public void Resolve_JsonMalformado_CaiNoDefaultSeguro()
    {
        var resultado = EmpresaAuthMethods.Resolve("{ isso nao e json valido");

        Assert.Single(resultado, EmpresaAuthMethods.Local);
    }

    [Fact]
    public void Resolve_ChaveAusente_CaiNoDefaultSeguro()
    {
        var resultado = EmpresaAuthMethods.Resolve("{\"outraCoisa\": true}");

        Assert.Single(resultado, EmpresaAuthMethods.Local);
    }

    [Fact]
    public void Resolve_SoEntraId_NaoRetornaLocal()
    {
        var resultado = EmpresaAuthMethods.Resolve("{\"authMethods\": [\"EntraId\"]}");

        Assert.Single(resultado, EmpresaAuthMethods.EntraId);
    }

    [Fact]
    public void Resolve_AmbosMetodos_RetornaOsDois()
    {
        var resultado = EmpresaAuthMethods.Resolve("{\"authMethods\": [\"Local\", \"EntraId\"]}");

        Assert.Equal(2, resultado.Count);
        Assert.Contains(EmpresaAuthMethods.Local, resultado);
        Assert.Contains(EmpresaAuthMethods.EntraId, resultado);
    }

    [Theory]
    [InlineData(null, EmpresaAuthMethods.Local, true)]
    [InlineData(null, EmpresaAuthMethods.EntraId, false)]
    [InlineData("{\"authMethods\": [\"EntraId\"]}", EmpresaAuthMethods.Local, false)]
    [InlineData("{\"authMethods\": [\"EntraId\"]}", EmpresaAuthMethods.EntraId, true)]
    [InlineData("{\"authMethods\": [\"Local\", \"EntraId\"]}", EmpresaAuthMethods.Local, true)]
    [InlineData("{\"authMethods\": [\"Local\", \"EntraId\"]}", EmpresaAuthMethods.EntraId, true)]
    public void Permite_CombinacoesEsperadas(string? configuracoes, string metodo, bool esperado)
    {
        Assert.Equal(esperado, EmpresaAuthMethods.Permite(configuracoes, metodo));
    }

    [Fact]
    public void Permite_ComparacaoIgnoraCaixa()
    {
        Assert.True(EmpresaAuthMethods.Permite("{\"authMethods\": [\"entraid\"]}", EmpresaAuthMethods.EntraId));
    }
}
