using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Rules
{
    public static class StatusAgendamentoRules
    {
        public static bool PodeTransitarPara(
            this StatusAgendamento atual,
            StatusAgendamento novo)
        {
            return atual switch
            {
                StatusAgendamento.Disponivel =>
                    novo == StatusAgendamento.Confirmado,

                StatusAgendamento.Pendente =>
                    novo == StatusAgendamento.Confirmado
                    || novo == StatusAgendamento.Cancelado,

                StatusAgendamento.Confirmado =>
                    novo == StatusAgendamento.EmAndamento
                    || novo == StatusAgendamento.Cancelado,

                StatusAgendamento.EmAndamento =>
                    novo == StatusAgendamento.Finalizado,
                _ => false
            };
        }
    }
}
