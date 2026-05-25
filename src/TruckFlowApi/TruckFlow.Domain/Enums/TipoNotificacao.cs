using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Enums
{
    public enum TipoNotificacao
    {
        AgendamentoCriado = 1,
        AgendamentoConfirmado = 2,
        AgendamentoCancelado = 3,
        AgendamentoReagendado = 4,
        AgendamentoExpirado = 5,
        MotoristaAtrasoInformado = 10,
        MotoristaChegou = 11,
        MotoristaSaiu = 12,
        JanelaPropxima = 20,
        MensagemManualAdmin = 30,
        MensagemManualMotorista = 31,
    }
}
