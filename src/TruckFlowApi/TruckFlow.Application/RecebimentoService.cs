using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RecebimentoService : IRecebimentoEventoService
    {
        private readonly IItemPlanejamentoRepositorio _itemRepo;
        private readonly IRecebimentoEventoRepositorio _eventoRepo;
        private readonly IPlanejamentoRecebimentoRepositorio _planejamentoRepo;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<RecebimentoService> _logger;

        public RecebimentoService(
            IItemPlanejamentoRepositorio itemRepo,
            IRecebimentoEventoRepositorio eventoRepo,
            IPlanejamentoRecebimentoRepositorio planejamentoRepo,
            IEmpresaRepositorio empresaRepo,
            ICurrentUserService currentUser,
            ILogger<RecebimentoService> logger)
        {
            _itemRepo = itemRepo;
            _eventoRepo = eventoRepo;
            _planejamentoRepo = planejamentoRepo;
            _empresaRepo = empresaRepo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task RegistrarRecebimentoManual(
         Guid itemPlanejamentoId,
         decimal quantidade,
         string? observacao,
         CancellationToken token
     )
        {
            var item = await _itemRepo.GetByIdWithPlanejamento(itemPlanejamentoId, token)
                ?? throw new NotFoundException("Item do planejamento não encontrado.");

            var empresaId = _currentUser.EmpresaId
                ?? throw new BusinessException("Usuário não vinculado a empresa.");

            var evento = new RecebimentoEvento(
                item,
                quantidade,
                agendamentoId: null,
                observacao,
                empresaId,
                tipo: TipoMovimentoRecebimento.Confirmacao
            );

            await _eventoRepo.AddAsync(evento, token);
            item.RegistrarRecebimento(quantidade);
            item.PlanejamentoRecebimento.RecalcularStatus();

            _logger.LogInformation(
                "Recebimento manual registrado: item {ItemId} qtd {Quantidade} (empresa {EmpresaId})",
                itemPlanejamentoId, quantidade, empresaId);
        }

        public async Task<PagedResponse<RecebimentoOrfaoDto>> GetOrfaos(
            RecebimentoOrfaoQueryDto query,
            CancellationToken token = default)
        {
            var paged = await _eventoRepo.GetOrfaosPagedAsync(query, token);

            return new PagedResponse<RecebimentoOrfaoDto>
            {
                Items = paged.Items.Select(o => new RecebimentoOrfaoDto
                {
                    Id = o.Id,
                    AgendamentoId = o.AgendamentoId,
                    ProdutoId = o.ProdutoId,
                    ProdutoNome = o.Produto?.Nome,
                    FornecedorId = o.FornecedorId,
                    FornecedorNome = o.Fornecedor?.Nome,
                    Quantidade = o.Quantidade,
                    DataRecebimento = o.DataRecebimento,
                    Observacao = o.Observacao
                }).ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages
            };
        }

        public async Task VincularOrfao(
            Guid eventoId,
            Guid itemPlanejamentoId,
            CancellationToken token = default)
        {
            var evento = await _eventoRepo.GetById(eventoId, token)
                ?? throw new NotFoundException("Evento de recebimento não encontrado.");

            if (!evento.EhOrfao)
                throw new BusinessException("Evento já está vinculado a um item de planejamento.");

            var item = await _itemRepo.GetByIdWithPlanejamento(itemPlanejamentoId, token)
                ?? throw new NotFoundException("Item de planejamento não encontrado.");

            if (evento.ProdutoId.HasValue && evento.ProdutoId.Value != item.ProdutoId)
            {
                throw new BusinessException(
                    "O produto do item de planejamento não bate com o produto do recebimento órfão.");
            }

            if (evento.Quantidade > item.SaldoDisponivel
                && evento.Tipo == TipoMovimentoRecebimento.Reserva)
            {
                throw new BusinessException(
                    $"Quantidade do órfão ({evento.Quantidade:N2}) excede saldo disponível " +
                    $"({item.SaldoDisponivel:N2}) do item escolhido.");
            }

            evento.Vincular(item);

            if (evento.Tipo == TipoMovimentoRecebimento.Reserva)
                item.Reservar(evento.Quantidade);
            else if (evento.Tipo == TipoMovimentoRecebimento.Confirmacao)
                item.RegistrarRecebimento(evento.Quantidade);

            item.PlanejamentoRecebimento.RecalcularStatus();
            await _eventoRepo.SaveChangeAsync(token);

            _logger.LogInformation(
                "Evento órfão {EventoId} vinculado ao item {ItemId}.",
                eventoId, itemPlanejamentoId);
        }
    }
}
