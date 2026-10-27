using TruckFlow.Domain.Entities;

namespace TruckFlow.Test.Domain;

public class EmpresaMetaDiariaTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"authMethods":["Local"]}""")]
    [InlineData("""{"metaDiariaKg":0}""")]
    [InlineData("""{"metaDiariaKg":-10}""")]
    [InlineData("nao-e-json")]
    public void ResolveKg_SemMetaValida_RetornaNull(string? configuracoes)
    {
        Assert.Null(EmpresaMetaDiaria.ResolveKg(configuracoes));
    }

    [Fact]
    public void Aplicar_PreservaOutrasChavesESubstituiMetaAntiga()
    {
        var resultado = EmpresaMetaDiaria.Aplicar("""{"authMethods":["Local","EntraId"],"MetaDiariaKg":1}""", 250000m);

        Assert.Equal(250000m, EmpresaMetaDiaria.ResolveKg(resultado));
        Assert.True(EmpresaAuthMethods.Permite(resultado, EmpresaAuthMethods.EntraId));
        Assert.DoesNotContain("\"MetaDiariaKg\"", resultado);
    }

    [Fact]
    public void Aplicar_Zero_RemoveAMetaEMantemOResto()
    {
        Assert.Null(EmpresaMetaDiaria.Aplicar("""{"metaDiariaKg":1000}""", 0m));

        var comAuth = EmpresaMetaDiaria.Aplicar("""{"authMethods":["Local"],"metaDiariaKg":1000}""", 0m);
        Assert.Null(EmpresaMetaDiaria.ResolveKg(comAuth));
        Assert.Contains("authMethods", comAuth);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nao-e-json")]
    public void Aplicar_SemConfiguracaoValida_CriaSoComAMeta(string? configuracoes)
    {
        Assert.Equal("""{"metaDiariaKg":1000}""", EmpresaMetaDiaria.Aplicar(configuracoes, 1000m));
    }

    [Fact]
    public void ResolveKg_ComMeta_RetornaValorJuntoComOutrasChaves()
    {
        Assert.Equal(480000m, EmpresaMetaDiaria.ResolveKg("""{"authMethods":["Local"],"metaDiariaKg":480000}"""));
    }
}
