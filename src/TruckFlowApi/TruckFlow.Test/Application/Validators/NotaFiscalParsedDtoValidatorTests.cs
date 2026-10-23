using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application.Validators;

public class NotaFiscalParsedDtoValidatorTests
{
    private readonly NotaFiscalParsedDtoValidator _validator = new();

    [Fact]
    public void Validate_DtoValido_PassaSemErros()
    {
        var dto = CriarDtoValido();

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456789012345678901234567890123")]
    [InlineData("123456789012345678901234567890123456789012345")]
    public void Validate_ChaveAcessoInvalida_RetornaErro(string chave)
    {
        var dto = CriarDtoValido();
        dto.ChaveAcesso = chave;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ChaveAcesso");
    }

    [Fact]
    public void Validate_ChaveAcessoComLetras_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.ChaveAcesso = new string('A', 44);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ChaveAcesso");
    }

    [Fact]
    public void Validate_NumeroZero_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.Numero = 0;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Numero");
    }

    [Fact]
    public void Validate_FornecedorComMenosDe3Chars_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.Fornecedor = "AB";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Fornecedor");
    }

    [Fact]
    public void Validate_DataEmissaoFutura_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.DataEmissao = DateTime.Now.AddDays(1);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DataEmissao");
    }

    [Fact]
    public void Validate_CnpjComFormatoErrado_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.EmitenteCnpj = "123.456.789/0001-00";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "EmitenteCnpj");
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("12345678000190")]
    public void Validate_DestinatarioCpfCnpjValido_PassaSemErro(string cpfCnpj)
    {
        var dto = CriarDtoValido();
        dto.DestinatarioCpfCnpj = cpfCnpj;

        var resultado = _validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == "DestinatarioCpfCnpj");
    }

    [Fact]
    public void Validate_DestinatarioCpfCnpjInvalido_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.DestinatarioCpfCnpj = "1234567";

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DestinatarioCpfCnpj");
    }

    [Fact]
    public void Validate_ValorTotalZero_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.ValorTotal = 0;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ValorTotal");
    }

    [Fact]
    public void Validate_PesoBrutoZero_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.PesoBruto = 0;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "PesoBruto");
    }

    [Fact]
    public void Validate_PesoBrutoNulo_PassaSemErro()
    {
        var dto = CriarDtoValido();
        dto.PesoBruto = null;

        var resultado = _validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == "PesoBruto");
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("BRA2E34")]
    public void Validate_PlacaNoFormatoValido_PassaSemErro(string placa)
    {
        var dto = CriarDtoValido();
        dto.PlacaVeiculo = placa;

        var resultado = _validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == "PlacaVeiculo");
    }

    [Theory]
    [InlineData("ABC-1234")]
    [InlineData("abc1d23")]
    [InlineData("ABCD123")]
    public void Validate_PlacaFormatoInvalido_RetornaErro(string placa)
    {
        var dto = CriarDtoValido();
        dto.PlacaVeiculo = placa;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "PlacaVeiculo");
    }

    [Fact]
    public void Validate_SemItens_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.Itens = new List<NotaFiscalItemDto>();

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens");
    }

    [Fact]
    public void Validate_ItemComQuantidadeZero_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.Itens = new List<NotaFiscalItemDto>
        {
            new() { Codigo = "001", Descricao = "Produto", ValorUnitario = 10, ValorTotal = 0, Quantidade = 0, Unidade = "KG" }
        };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens");
    }

    private static NotaFiscalParsedDto CriarDtoValido() =>
        new()
        {
            ChaveAcesso = new string('1', 44),
            Numero = 1001,
            Fornecedor = "Fornecedor Teste Ltda",
            Serie = "001",
            DataEmissao = DateTime.Now.AddDays(-1),
            EmitenteNome = "Emitente Teste Ltda",
            EmitenteCnpj = "12345678000190",
            DestinatarioNome = "Aurora Alimentos",
            DestinatarioCpfCnpj = "98765432000101",
            ValorTotal = 1000m,
            PesoBruto = 500m,
            PlacaVeiculo = "ABC1D23",
            TipoCarga = TipoCarga.Milho,
            Itens = new List<NotaFiscalItemDto>
            {
                new()
                {
                    Codigo = "PROD-001",
                    Descricao = "Produto de Teste",
                    ValorUnitario = 100m,
                    ValorTotal = 1000m,
                    Quantidade = 10,
                    Unidade = "KG"
                }
            }
        };
}
