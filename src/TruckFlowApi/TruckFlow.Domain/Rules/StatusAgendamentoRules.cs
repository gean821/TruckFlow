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
                // Vaga aberta só pode ser reservada
                StatusAgendamento.Disponivel =>
                novo == StatusAgendamento.Agendado
                || novo == StatusAgendamento.Cancelado,

                // (Opcional) Se existir fluxo pendente
                StatusAgendamento.Pendente =>
                    novo == StatusAgendamento.Agendado
                    || novo == StatusAgendamento.Cancelado,

                // Caminhão a caminho
                StatusAgendamento.Agendado =>
                    novo == StatusAgendamento.EmAndamento   // Check-in
                    || novo == StatusAgendamento.Cancelado, // No-show

                // Caminhão na doca
                StatusAgendamento.EmAndamento =>
                    novo == StatusAgendamento.Finalizado    // Check-out
                    || novo == StatusAgendamento.Cancelado, // Rejeição

                // Estados finais
                StatusAgendamento.Finalizado =>
                    false,

                StatusAgendamento.Cancelado =>
                    false,

                _ => false
            };
        }
    }
}
