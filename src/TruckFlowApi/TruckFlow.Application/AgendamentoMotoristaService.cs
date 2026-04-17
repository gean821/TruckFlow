using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
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

        public AgendamentoMotoristaService(
            IAgendamentoRepositorio repo,
            INotaFiscalRepositorio notaRepo,
            IPlanejamentoRecebimentoRepositorio planejamentoRepo,
            IRecebimentoEventoRepositorio eventoRepo
            )
        {
            _repo = repo;
            _notaRepo = notaRepo;
            _planejamentoRepo = planejamentoRepo;
            _eventoRepo = eventoRepo;
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetDriverAppointments(
            Guid motoristaId,
            CancellationToken token = default)
        {
            var meusAgendamentos = await _repo.GetByMotoristaId(motoristaId, token);

            if (meusAgendamentos.Count == 0)
            {
                return [];
            }

            return meusAgendamentos.Select(MapToResponse).ToList();
        }

        public async Task<AgendamentoMotoristaResponse> BookAppointment
            (
                ReservarAgendamentoDto dto,
                CancellationToken token = default
            )
        {
            var vaga = await _repo.GetById(dto.AgendamentoId, token)
                ?? throw new NotFoundException("Horário não encontrado para o agendamento.");

            var nota = await _notaRepo.ObterPorChaveAsync(dto.NotaFiscalChaveAcesso, token)
                ?? throw new NotFoundException("Nota Fiscal não encontrada.");

            var produtoId = vaga.Grade?.ProdutoId ?? vaga.ProdutoId;

            PlanejamentoRecebimento? planejamento = null;
            ItemPlanejamento? item = null;

            if (produtoId.HasValue)
            {
                planejamento = await _planejamentoRepo.GetPlanejamentoAtivoPorFornecedor(
                    vaga.FornecedorId, token);

                item = planejamento?.ItemDoProduto(produtoId.Value);

                if (planejamento is not null && item is not null &&
                    item.MetaDiariaAtingida(vaga.DataInicio))
                {
                    throw new BusinessException(
                        "Meta diária atingida para este produto. Grade congelada.");
                }
            }

            vaga.Reservar(
                usuarioId: dto.UsuarioId,
                notaFiscal: nota,
                placaVeiculo: dto.PlacaVeiculo,
                tipoVeiculo: dto.TipoVeiculo
            );

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

            return MapToResponse(vaga);
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetAvailableAppointments
            (
                Guid fornecedorId,
                DateTime data,
                CancellationToken token = default
            )
        {
            var inicioDia = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
            var fimDia = inicioDia.AddDays(1).AddTicks(-1);

            var vagas = await _repo.GetAvailable(
                fornecedorId,
                inicioDia,
                fimDia,
                token
            );

            var planejamento = await _planejamentoRepo.GetPlanejamentoAtivoPorFornecedor(
                fornecedorId, token);

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
