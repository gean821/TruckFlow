using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Test.Application.Validators;

public class NotaFiscalItemDtoValidatorTests
{
    private readonly NotaFiscalItemDtoValidator _validator = new();

    [Fact]
    public void Validate_ItemValido_PassaSemErros()
    {
        var dto = CriarItemValido();

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validate_CodigoVazio_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Codigo = "";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoMaiorQue60Chars_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Codigo = new string('A', 61);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_DescricaoComMenosDe3Chars_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Descricao = "AB";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Descricao");
    }

    [Fact]
    public void Validate_QuantidadeZero_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Quantidade = 0;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Quantidade");
    }

    [Fact]
    public void Validate_UnidadeMaiorQue6Chars_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Unidade = "KILOGRAM";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Unidade");
    }

    [Fact]
    public void Validate_ValorUnitarioZero_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.ValorUnitario = 0;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ValorUnitario");
    }

    [Fact]
    public void Validate_ValorTotalDivergeDaMultiplicacao_RetornaErro()
    {
        var dto = CriarItemValido();
        dto.Quantidade = 10;
        dto.ValorUnitario = 100;
        dto.ValorTotal = 999;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ValorTotal");
    }

    [Fact]
    public void Validate_ValorTotalIgualAQuantidadeVezesUnitario_PassaSemErro()
    {
        var dto = CriarItemValido();
        dto.Quantidade = 5;
        dto.ValorUnitario = 200;
        dto.ValorTotal = 1000;

        var resultado = _validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == "ValorTotal");
    }

    private static NotaFiscalItemDto CriarItemValido() =>
        new()
        {
            Codigo = "PROD-001",
            Descricao = "Produto de Teste",
            Quantidade = 10,
            Unidade = "KG",
            ValorUnitario = 100m,
            ValorTotal = 1000m
        };
}
