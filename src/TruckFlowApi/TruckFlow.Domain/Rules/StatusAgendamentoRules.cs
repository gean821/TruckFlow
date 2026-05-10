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
                    novo == StatusAgendamento.Agendado
                    || novo == StatusAgendamento.Cancelado
                    || novo == StatusAgendamento.Expirado,

                StatusAgendamento.Pendente =>
                    novo == StatusAgendamento.Agendado
                    || novo == StatusAgendamento.Cancelado
                    || novo == StatusAgendamento.Expirado,

                StatusAgendamento.Agendado =>
                    novo == StatusAgendamento.EmAndamento
                    || novo == StatusAgendamento.Cancelado
                    || novo == StatusAgendamento.Expirado,

                StatusAgendamento.EmAndamento =>
                    novo == StatusAgendamento.Finalizado
                    || novo == StatusAgendamento.Cancelado,

                StatusAgendamento.Expirado =>
                    novo == StatusAgendamento.Finalizado
                    || novo == StatusAgendamento.Cancelado,

                StatusAgendamento.Finalizado => false,
                StatusAgendamento.Cancelado => false,

                _ => false
            };
        }
    }
}
