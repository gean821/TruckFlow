using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AgendamentoRecebimentoLifecycleService : IAgendamentoRecebimentoLifecycleService
    {
        private readonly IPlanejamentoRecebimentoRepositorio _planejamentoRepo;
        private readonly IRecebimentoEventoRepositorio _eventoRepo;
        private readonly ILogger<AgendamentoRecebimentoLifecycleService> _logger;

        public AgendamentoRecebimentoLifecycleService(
            IPlanejamentoRecebimentoRepositorio planejamentoRepo,
            IRecebimentoEventoRepositorio eventoRepo,
            ILogger<AgendamentoRecebimentoLifecycleService> logger)
        {
            _planejamentoRepo = planejamentoRepo;
            _eventoRepo = eventoRepo;
            _logger = logger;
        }

        public async Task AoReservarAsync(
            Agendamento agendamento,
            decimal pesoEstimado,
            CancellationToken token = default)
        {
            if (pesoEstimado <= 0)
            {
                _logger.LogWarning(
                    "Reserva sem peso estimado para agendamento {AgendamentoId}. Nenhum evento criado.",
                    agendamento.Id);
                return;
            }

            var reservaExistente = await _eventoRepo.GetByAgendamentoIdETipo(
                agendamento.Id,
                TipoMovimentoRecebimento.Reserva,
                token);

            if (reservaExistente is not null)
            {
                _logger.LogInformation(
                    "Reserva idempotente: agendamento {AgendamentoId} já possui evento de reserva.",
                    agendamento.Id);
                return;
            }

            var produtoId = agendamento.Grade?.ProdutoId ?? agendamento.ProdutoId;
            var fornecedorId = agendamento.FornecedorId
                ?? agendamento.NotaFiscal?.FornecedorId;

            var (planejamento, item) = await ResolverPlanejamentoEItemAsync(
                produtoId, fornecedorId, token);

            if (item is null)
            {
                var orfao = RecebimentoEvento.CriarOrfao(
                    produtoId,
                    fornecedorId,
                    pesoEstimado,
                    agendamento.Id,
                    "Reserva órfã: sem planejamento ativo no momento da reserva.",
                    agendamento.EmpresaId);

                await _eventoRepo.AddAsync(orfao, token);

                _logger.LogWarning(
                    "Reserva órfã criada para agendamento {AgendamentoId} (empresa {EmpresaId}) — vincular manualmente.",
                    agendamento.Id, agendamento.EmpresaId);
                return;
            }

            if (pesoEstimado > item.SaldoDisponivel)
            {
                throw new BusinessException(
                    $"Quantidade reservada ({pesoEstimado:N2}) excede o saldo disponível " +
                    $"({item.SaldoDisponivel:N2}) do planejamento para este produto.");
            }

            var evento = new RecebimentoEvento(
                item,
                pesoEstimado,
                agendamentoId: agendamento.Id,
                observacao: "Reserva via agendamento",
                empresaId: agendamento.EmpresaId,
                tipo: TipoMovimentoRecebimento.Reserva
            );

            await _eventoRepo.AddAsync(evento, token);

            item.Reservar(pesoEstimado);
            planejamento!.RecalcularStatus();
            await _planejamentoRepo.SaveChangesAsync(token);
        }

        public async Task AoCancelarOuExpirarAsync(
            Agendamento agendamento,
            CancellationToken token = default)
        {
            var eventos = await _eventoRepo.GetByAgendamentoIdAll(agendamento.Id, token);
            if (eventos.Count == 0)
                return;

            foreach (var ev in eventos.OrderBy(e => e.CreatedAt))
            {
                if (ev.ItemPlanejamento is null)
                {
                    await _eventoRepo.Remove(ev, token);
                    continue;
                }

                if (ev.Tipo == TipoMovimentoRecebimento.Reserva)
                {
                    ev.ItemPlanejamento.EstornarReserva(ev.Quantidade);
                }
                else if (ev.Tipo == TipoMovimentoRecebimento.Confirmacao)
                {
                    ev.ItemPlanejamento.EstornarRecebimento(ev.Quantidade);
                }

                ev.ItemPlanejamento.PlanejamentoRecebimento?.RecalcularStatus();
                await _eventoRepo.Remove(ev, token);
            }
        }

        public async Task AoFinalizarAsync(
            Agendamento agendamento,
            decimal quantidadeRealRecebida,
            CancellationToken token = default)
        {
            if (quantidadeRealRecebida <= 0)
                throw new BusinessException("Quantidade recebida inválida.");

            var reserva = await _eventoRepo.GetByAgendamentoIdETipo(
                agendamento.Id,
                TipoMovimentoRecebimento.Reserva,
                token);

            var confirmacaoExistente = await _eventoRepo.GetByAgendamentoIdETipo(
                agendamento.Id,
                TipoMovimentoRecebimento.Confirmacao,
                token);

            if (confirmacaoExistente is not null)
            {
                _logger.LogInformation(
                    "Confirmação idempotente: agendamento {AgendamentoId} já possui evento de confirmação.",
                    agendamento.Id);
                return;
            }

            ItemPlanejamento? item = reserva?.ItemPlanejamento;
            decimal qtdReservadaOriginal = reserva?.Quantidade ?? 0m;

            if (item is null)
            {
                var produtoId = agendamento.Grade?.ProdutoId ?? agendamento.ProdutoId;
                var fornecedorId = agendamento.FornecedorId
                    ?? agendamento.NotaFiscal?.FornecedorId;

                var (planejamento, itemResolvido) = await ResolverPlanejamentoEItemAsync(
                    produtoId, fornecedorId, token);

                if (itemResolvido is null)
                {
                    var confirmOrfa = RecebimentoEvento.CriarOrfao(
                        produtoId,
                        fornecedorId,
                        quantidadeRealRecebida,
                        agendamento.Id,
                        "Confirmação órfã: sem planejamento ativo no momento do recebimento.",
                        agendamento.EmpresaId);

                    await _eventoRepo.AddAsync(confirmOrfa, token);

                    _logger.LogWarning(
                        "Confirmação órfã para agendamento {AgendamentoId}.", agendamento.Id);
                    return;
                }

                item = itemResolvido;
            }

            if (reserva is not null)
            {
                await _eventoRepo.Remove(reserva, token);
            }

            var confirmacao = new RecebimentoEvento(
                item,
                quantidadeRealRecebida,
                agendamentoId: agendamento.Id,
                observacao: "Confirmação de recebimento",
                empresaId: agendamento.EmpresaId,
                tipo: TipoMovimentoRecebimento.Confirmacao
            );

            await _eventoRepo.AddAsync(confirmacao, token);

            item.ConfirmarRecebimento(quantidadeRealRecebida, qtdReservadaOriginal);
            item.PlanejamentoRecebimento?.RecalcularStatus();
            await _planejamentoRepo.SaveChangesAsync(token);
        }

        public async Task AoAtualizarReservaAsync(
            Agendamento agendamento,
            decimal novoPesoEstimado,
            CancellationToken token = default)
        {
            var reserva = await _eventoRepo.GetByAgendamentoIdETipo(
                agendamento.Id,
                TipoMovimentoRecebimento.Reserva,
                token);

            if (reserva is null)
            {
                if (novoPesoEstimado > 0)
                    await AoReservarAsync(agendamento, novoPesoEstimado, token);
                return;
            }

            if (reserva.Quantidade == novoPesoEstimado)
                return;

            await AoCancelarOuExpirarAsync(agendamento, token);

            if (novoPesoEstimado > 0)
                await AoReservarAsync(agendamento, novoPesoEstimado, token);
        }

        private async Task<(PlanejamentoRecebimento? planejamento, ItemPlanejamento? item)>
            ResolverPlanejamentoEItemAsync(
                Guid? produtoId,
                Guid? fornecedorId,
                CancellationToken token)
        {
            if (!produtoId.HasValue || !fornecedorId.HasValue)
                return (null, null);

            var planejamento = await _planejamentoRepo.GetPlanejamentoAtivoPorFornecedor(
                fornecedorId.Value, token);

            var item = planejamento?.ItemDoProduto(produtoId.Value);
            return (planejamento, item);
        }
    }
}
