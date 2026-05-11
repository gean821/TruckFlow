using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;


namespace TruckFlow.Application
{
    public class AgendamentoMotoristaService : IAgendamentoMotoristaService
    {
        private readonly IAgendamentoRepositorio _repo;
        private readonly INotaFiscalRepositorio _notaRepo;
        private readonly IPlanejamentoRecebimentoRepositorio _planejamentoRepo;
        private readonly IRecebimentoEventoRepositorio _eventoRepo;
        private readonly ILogger<AgendamentoMotoristaService> _logger;
        private readonly CurrentUserGuard _guard;


        public AgendamentoMotoristaService(
            IAgendamentoRepositorio repo,
            INotaFiscalRepositorio notaRepo,
            IPlanejamentoRecebimentoRepositorio planejamentoRepo,
            IRecebimentoEventoRepositorio eventoRepo,
            ILogger<AgendamentoMotoristaService> logger,
            CurrentUserGuard guard
            )
        {
            _repo = repo;
            _notaRepo = notaRepo;
            _planejamentoRepo = planejamentoRepo;
            _eventoRepo = eventoRepo;
            _logger = logger;
            _guard = guard;
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetDriverAppointments(
            CancellationToken token = default)
        {
            var motoristaId = _guard.GetMotoristaId();

            var meusAgendamentos = await _repo.GetByMotoristaId(motoristaId, token)
                ?? throw new NotFoundException("Nenhum agendamento para o motorista.");

            return meusAgendamentos.Select(MapToResponse).ToList();
        }

        public async Task<AgendamentoMotoristaResponse> BookAppointment
            (
                ReservarAgendamentoDto dto,
                CancellationToken token = default
            )
        {

            var usuarioId = _guard.GetUserId();

            var vaga = await _repo.GetById(dto.AgendamentoId, token)
                ?? throw new NotFoundException("Horário não encontrado para o agendamento.");

            var nota = await _notaRepo.ObterPorChaveAsync(dto.NotaFiscalChaveAcesso, token)
                ?? throw new NotFoundException("Nota Fiscal não encontrada.");

            var produtoId = vaga.Grade?.ProdutoId ?? vaga.ProdutoId;
            var fornecedorIdParaPlanejamento = vaga.FornecedorId ?? nota.FornecedorId;

            PlanejamentoRecebimento? planejamento = null;
            ItemPlanejamento? item = null;

            if (produtoId.HasValue)
            {
                planejamento = await _planejamentoRepo.GetPlanejamentoAtivoPorFornecedor(
                    fornecedorIdParaPlanejamento, token);

                item = planejamento?.ItemDoProduto(produtoId.Value);

                if (planejamento is not null && item is not null &&
                    item.MetaDiariaAtingida(vaga.DataInicio))
                {
                    throw new BusinessException(
                        "Meta diária atingida para este produto. Grade congelada.");
                }
            }

            if (vaga.LocalDescargaId.HasValue)
            {
                var conflitos = await _repo.GetConflitosAsync(
                    vaga.LocalDescargaId.Value,
                    vaga.DataInicio,
                    vaga.DataFim,
                    excludeAgendamentoId: vaga.Id,
                    token);

                if (conflitos.Count > 0)
                {
                    throw new BusinessException(
                        "Esta vaga conflita com outro agendamento ativo no mesmo horário e doca. " +
                        "Por favor, escolha outro horário.");
                }
            }

            vaga.Reservar(
                usuarioId: usuarioId,
                notaFiscal: nota,
                placaVeiculo: dto.PlacaVeiculo,
                tipoVeiculo: dto.TipoVeiculo
            );

            await using var tx = await _repo.BeginTransactionAsync(token);
            try
            {
                await _repo.Update(vaga, token);

                if (item is not null)
                {
                    var quantidade = nota.PesoBruto ?? 0m;
                    if (quantidade > 0)
                    {
                        var evento = new RecebimentoEvento(
                            item,
                            quantidade,
                            agendamentoId: vaga.Id,
                            observacao: "Reserva via agendamento",
                            empresaId: vaga.EmpresaId
                        );

                        await _eventoRepo.AddAsync(evento, token);

                        item.RegistrarRecebimento(quantidade);
                        planejamento!.RecalcularStatus();
                        await _planejamentoRepo.SaveChangesAsync(token);
                    }
                }

                await tx.CommitAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessException(
                    "Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23P01")
            {
                throw new BusinessException(
                    "Conflito de horário detectado: outro agendamento ativo sobrepõe esta vaga. " +
                    "Atualize a lista e escolha outra.");
            }

            _logger.LogInformation(
                "Agendamento reservado pelo motorista: {AgendamentoId} (usuário {UsuarioId}, empresa {EmpresaId})",
                vaga.Id, usuarioId, vaga.EmpresaId);

            return MapToResponse(vaga);
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetAvailableAppointments
            (
                string chaveAcesso,
                DateTime data,
                CancellationToken token = default
            )
        {
            var nota = await _notaRepo.ObterPorChaveAsync(chaveAcesso, token)
                ?? throw new NotFoundException("Nota fiscal não encontrada. Faça o upload primeiro.");

            var produtoIds = nota.Itens
                .Where(i => i.ProdutoId.HasValue)
                .Select(i => i.ProdutoId!.Value)
                .Distinct()
                .ToList();

            if (produtoIds.Count == 0)
            {
                _logger.LogInformation(
                    "Nota {ChaveAcesso} não tem itens vinculados a produtos cadastrados — sem vagas para retornar.",
                    chaveAcesso);
                return new List<AgendamentoMotoristaResponse>();
            }

            var inicioDiaLocal = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified);
            var fimDiaLocal = inicioDiaLocal.AddDays(1).AddTicks(-1);
            var inicioDia = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, Grade.OperationalTimeZone);
            var fimDia = TimeZoneInfo.ConvertTimeToUtc(fimDiaLocal, Grade.OperationalTimeZone);

            var vagas = await _repo.GetAvailableByProdutos(
                produtoIds,
                nota.FornecedorId,
                inicioDia,
                fimDia,
                nota.EmpresaId,
                token
            );

            var planejamento = await _planejamentoRepo.GetPlanejamentoAtivoPorFornecedor(
                nota.FornecedorId, token);

            return vagas
                .Select(v => MapToResponse(v, planejamento, data))
                .ToList();
        }

        private AgendamentoMotoristaResponse MapToResponse(Agendamento agendamento)
            => MapToResponse(agendamento, planejamento: null, dataReferencia: null);

        private AgendamentoMotoristaResponse MapToResponse(
            Agendamento agendamento,
            PlanejamentoRecebimento? planejamento,
            DateTime? dataReferencia)
        {
            string produtoNome = agendamento.Grade?.Produto?.Nome ?? "Carga Geral";
            string unidadeNome = agendamento.UnidadeEntrega?.Nome ?? "A Definir";

            var produtoId = agendamento.Grade?.ProdutoId ?? agendamento.ProdutoId;
            var dia = dataReferencia ?? agendamento.DataInicio;

            var congelado = produtoId.HasValue
                && planejamento is not null
                && planejamento.DeveCongelarProduto(produtoId.Value, dia);

            return new AgendamentoMotoristaResponse
            {
                Id = agendamento.Id,
                UnidadeDescarga = unidadeNome,
                Fornecedor = agendamento.Fornecedor?.Nome ?? "N/A",
                HorarioInicio = agendamento.DataInicio,
                Produto = produtoNome,
                PesoCarga = agendamento.VolumeCarga > 0 ? agendamento.VolumeCarga : null,
                NotaFiscal = agendamento.NotaFiscal?.Numero.ToString() ?? "-",
                PlacaVeiculo = agendamento.NotaFiscal?.PlacaVeiculo ?? agendamento.PlacaVeiculo ?? "-",
                CreatedAt = agendamento.CreatedAt,
                Status = agendamento.StatusAgendamento.ToString(),
                Congelado = congelado,
                MotivoCongelamento = congelado
                    ? "Meta diária atingida para este produto."
                    : null
            };
        }
    }
}
