using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
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

        public AgendamentoMotoristaService(
            IAgendamentoRepositorio repo,
            INotaFiscalRepositorio notaRepo
            )
        {
            _repo = repo;
            _notaRepo = notaRepo;
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
            var vaga = await _repo.GetById(dto.AgendamentoId,token)
                ?? throw new NotFoundException("Horário não encontrado para o agendamento.");

            var nota = await _notaRepo.ObterPorChaveAsync(dto.NotaFiscalChaveAcesso, token)
                ?? throw new NotFoundException("Nota Fiscal não encontrada.");

            vaga.Reservar(
                usuarioId: dto.UsuarioId,
                notaFiscal: nota,
                placaVeiculo: dto.PlacaVeiculo,
                tipoVeiculo: dto.TipoVeiculo
            );


            await _repo.Update(vaga, token);

            return MapToResponse(vaga);
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetAvailableAppointments
            (
                Guid fornecedorId,
                DateTime data,
                CancellationToken token = default
            )
        {

            var inicioDia = DateTime.SpecifyKind(data.Date,DateTimeKind.Utc);

            var fimDia = inicioDia
                .AddDays(1)
                .AddTicks(-1);

            var vagas = await _repo.GetAvailable(
                fornecedorId,
                inicioDia,
                fimDia,
                token
            );

            return vagas.Select(MapToResponse).ToList();
        }

        private AgendamentoMotoristaResponse MapToResponse(Agendamento agendamento)
        {

            string produtoNome = agendamento.Grade?.Produto?.Nome ?? "Carga Geral";
            string unidadeNome = agendamento.UnidadeEntrega?.Nome ?? "A Definir";

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
                Status = agendamento.StatusAgendamento.ToString()
            };
        }

    }
}




