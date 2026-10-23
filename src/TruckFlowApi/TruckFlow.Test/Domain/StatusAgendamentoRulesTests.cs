using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Test.Domain;

public class StatusAgendamentoRulesTests
{
    [Theory]
    [InlineData(StatusAgendamento.Agendado,    true)]
    [InlineData(StatusAgendamento.Cancelado,   true)]
    [InlineData(StatusAgendamento.Expirado,    true)]
    [InlineData(StatusAgendamento.EmAndamento, false)]
    [InlineData(StatusAgendamento.Finalizado,  false)]
    [InlineData(StatusAgendamento.Pendente,    false)]
    public void Disponivel_TransicoesPermitidas(StatusAgendamento para, bool esperado)
    {
        Assert.Equal(esperado, StatusAgendamento.Disponivel.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.Agendado,    true)]
    [InlineData(StatusAgendamento.Cancelado,   true)]
    [InlineData(StatusAgendamento.Expirado,    true)]
    [InlineData(StatusAgendamento.EmAndamento, false)]
    [InlineData(StatusAgendamento.Finalizado,  false)]
    [InlineData(StatusAgendamento.Disponivel,  false)]
    public void Pendente_TransicoesPermitidas(StatusAgendamento para, bool esperado)
    {
        Assert.Equal(esperado, StatusAgendamento.Pendente.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.EmAndamento, true)]
    [InlineData(StatusAgendamento.Cancelado,   true)]
    [InlineData(StatusAgendamento.Expirado,    true)]
    [InlineData(StatusAgendamento.Finalizado,  false)]
    [InlineData(StatusAgendamento.Disponivel,  false)]
    [InlineData(StatusAgendamento.Pendente,    false)]
    public void Agendado_TransicoesPermitidas(StatusAgendamento para, bool esperado)
    {
        Assert.Equal(esperado, StatusAgendamento.Agendado.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.Finalizado, true)]
    [InlineData(StatusAgendamento.Cancelado,  true)]
    [InlineData(StatusAgendamento.Agendado,   false)]
    [InlineData(StatusAgendamento.Disponivel, false)]
    [InlineData(StatusAgendamento.Expirado,   false)]
    [InlineData(StatusAgendamento.Pendente,   false)]
    public void EmAndamento_TransicoesPermitidas(StatusAgendamento para, bool esperado)
    {
        Assert.Equal(esperado, StatusAgendamento.EmAndamento.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.Finalizado,  true)]
    [InlineData(StatusAgendamento.Cancelado,   true)]
    [InlineData(StatusAgendamento.Agendado,    false)]
    [InlineData(StatusAgendamento.Disponivel,  false)]
    [InlineData(StatusAgendamento.EmAndamento, false)]
    [InlineData(StatusAgendamento.Pendente,    false)]
    public void Expirado_TransicoesPermitidas(StatusAgendamento para, bool esperado)
    {
        Assert.Equal(esperado, StatusAgendamento.Expirado.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.Disponivel)]
    [InlineData(StatusAgendamento.Pendente)]
    [InlineData(StatusAgendamento.Agendado)]
    [InlineData(StatusAgendamento.EmAndamento)]
    [InlineData(StatusAgendamento.Expirado)]
    [InlineData(StatusAgendamento.Cancelado)]
    public void Finalizado_NaoPodeTransitarParaNenhumStatus(StatusAgendamento para)
    {
        Assert.False(StatusAgendamento.Finalizado.PodeTransitarPara(para));
    }

    [Theory]
    [InlineData(StatusAgendamento.Disponivel)]
    [InlineData(StatusAgendamento.Pendente)]
    [InlineData(StatusAgendamento.Agendado)]
    [InlineData(StatusAgendamento.EmAndamento)]
    [InlineData(StatusAgendamento.Expirado)]
    [InlineData(StatusAgendamento.Finalizado)]
    public void Cancelado_NaoPodeTransitarParaNenhumStatus(StatusAgendamento para)
    {
        Assert.False(StatusAgendamento.Cancelado.PodeTransitarPara(para));
    }
}
