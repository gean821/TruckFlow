using TruckFlow.Application.Validators.Grade;
using TruckFlow.Domain.Dto.Grade;

namespace TruckFlow.Test.Application.Validators;

public class GradeUpdateValidatorTests
{
    private readonly GradeUpdateValidator _validator = new();

    [Fact]
    public void Validate_DtoVazio_PassaSemErros()
    {
        var dto = new GradeUpdateDto();

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }


    [Fact]
    public void Validate_HoraFinalMenorQueHoraInicial_ViradaDeMeiaNoite_PassaSemErro()
    {
        var dto = new GradeUpdateDto
        {
            HoraInicial = new TimeOnly(22, 0),
            HoraFinal = new TimeOnly(2, 0),
        };

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validate_HoraFinalIgualAHoraInicial_RetornaErro()
    {
        var dto = new GradeUpdateDto
        {
            HoraInicial = new TimeOnly(8, 0),
            HoraFinal = new TimeOnly(8, 0),
        };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "HoraFinal");
    }

    [Fact]
    public void Validate_ApenasHoraFinalEnviada_NaoValidaCruzamentoAqui()
    {
        var dto = new GradeUpdateDto { HoraFinal = new TimeOnly(8, 0) };

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid);
    }
}
