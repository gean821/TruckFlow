using TruckFlow.Application.Validators.Grade;
using TruckFlow.Domain.Dto.Grade;

namespace TruckFlow.Test.Application.Validators;

public class GradeCreateValidatorTests
{
    private readonly GradeCreateValidator _validator = new();

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
        dto.DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        dto.DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DataInicio");
    }

    [Fact]
    public void Validate_DataInicioHoje_PassaSemErroDeData()
    {
        var dto = CriarDtoValido();
        dto.DataInicio = DateOnly.FromDateTime(DateTime.Today);
        dto.DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var resultado = _validator.Validate(dto);

        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == "DataInicio");
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
    public void Validate_IntervaloMaiorQueJanela_RetornaErro()
    {
        var dto = CriarDtoValido();
        dto.HoraInicial = new TimeOnly(8, 0);
        dto.HoraFinal = new TimeOnly(9, 0);
        dto.IntervaloMinutos = 120;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "IntervaloMinutos");
    }

    [Fact]
    public void Validate_IntervaloIgualAJanela_PassaSemErro()
    {
        var dto = CriarDtoValido();
        dto.HoraInicial = new TimeOnly(8, 0);
        dto.HoraFinal = new TimeOnly(9, 0);
        dto.IntervaloMinutos = 60;

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validate_IntervaloCabeComViradaDeMeiaNoite_PassaSemErro()
    {
        var dto = CriarDtoValido();
        dto.HoraInicial = new TimeOnly(22, 0);
        dto.HoraFinal = new TimeOnly(2, 0);
        dto.IntervaloMinutos = 60;

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    private static GradeCreateDto CriarDtoValido() =>
        new()
        {
            ProdutoId = Guid.NewGuid(),
            LocalDescargaId = Guid.NewGuid(),
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            HoraInicial = new TimeOnly(8, 0),
            HoraFinal = new TimeOnly(17, 0),
            IntervaloMinutos = 60,
            DiasSemana = "0,1,2,3,4,5,6",
        };
}
