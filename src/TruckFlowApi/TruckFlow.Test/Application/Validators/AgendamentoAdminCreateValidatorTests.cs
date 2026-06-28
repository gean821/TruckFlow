using TruckFlow.Application.Validators.AgendamentoMotorista;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Application.Validators;

public class AgendamentoAdminCreateValidatorTests
{
    private readonly AgendamentoAdminCreateValidator _validator = new();

    [Fact]
    public void Validate_DtoValido_PassaSemErros()
    {
        var dto = CriarDtoValido();

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validate_DataInicioNoPassado_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.DataInicio = DateTime.Now.AddHours(-2);
        dto.DataFim = DateTime.Now.AddHours(-1);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DataInicio");
    }

    [Fact]
    public void Validate_DataFimAntesDaDataInicio_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.DataFim = dto.DataInicio.AddHours(-1);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DataFim");
    }

    [Fact]
    public void Validate_LocalDescargaIdVazio_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.LocalDescargaId = Guid.Empty;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "LocalDescargaId");
    }

    [Fact]
    public void Validate_ProdutoIdNulo_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.ProdutoId = null;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ProdutoId");
    }

    private static AgendamentoAdminCreateDto CriarDtoValido() =>
        new()
        {
            DataInicio = DateTime.Now.AddHours(2),
            DataFim = DateTime.Now.AddHours(4),
            LocalDescargaId = Guid.NewGuid(),
            ProdutoId = Guid.NewGuid(),
            TipoCarga = TipoCarga.Milho
        };
}
