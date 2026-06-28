using TruckFlow.Application.Validators.EanValidators;

namespace TruckFlow.Test.Application;

public class EanValidatorTests
{
    [Theory]
    [InlineData("7891000315507")]
    [InlineData("7896045507009")]
    [InlineData("4006381333931")]
    public void IsValid_Ean13Valido_RetornaTrue(string ean)
    {
        Assert.True(EanValidator.IsValid(ean));
    }

    [Theory]
    [InlineData("96385074")]
    [InlineData("40170725")]
    public void IsValid_Ean8Valido_RetornaTrue(string ean)
    {
        Assert.True(EanValidator.IsValid(ean));
    }

    [Theory]
    [InlineData("7891000315508")]
    [InlineData("7896045507002")]
    public void IsValid_DigitoVerificadorErrado_RetornaFalse(string ean)
    {
        Assert.False(EanValidator.IsValid(ean));
    }

    [Theory]
    [InlineData("789100031550")]
    [InlineData("789100031")]
    [InlineData("123456789")]
    public void IsValid_TamanhoInvalido_RetornaFalse(string ean)
    {
        Assert.False(EanValidator.IsValid(ean));
    }

    [Theory]
    [InlineData("789100031550A")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("789-1000-31550")]
    public void IsValid_CaracteresNaoNumericos_RetornaFalse(string ean)
    {
        Assert.False(EanValidator.IsValid(ean));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_StringVaziaOuEspacos_RetornaFalse(string ean)
    {
        Assert.False(EanValidator.IsValid(ean));
    }
}
