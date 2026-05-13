using System;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoRecebimentoLifecycleService
    {
        Task AoReservarAsync(
            Agendamento agendamento,
            decimal pesoEstimado,
            CancellationToken token = default);

        Task AoCancelarOuExpirarAsync(
            Agendamento agendamento,
            CancellationToken token = default);

        Task AoFinalizarAsync(
            Agendamento agendamento,
            decimal quantidadeRealRecebida,
            CancellationToken token = default);

        Task AoAtualizarReservaAsync(
            Agendamento agendamento,
            decimal novoPesoEstimado,
            CancellationToken token = default);
    }
}
