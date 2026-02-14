using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class GradeService : IGradeService
    {
        private readonly IGradeRepositorio _repo;
        private readonly IValidator<GradeCreateDto> _createValidator;
        private readonly IValidator<GradeUpdateDto> _updateValidator;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IUnidadeEntregaRepositorio _unidadeRepo;
        private readonly IAgendamentoRepositorio _agendamentoRepo;
        private readonly GradeFactory _factory;


        public GradeService(
            IGradeRepositorio repo,
            IValidator<GradeCreateDto> createValidator,
            IValidator<GradeUpdateDto> updateValidator,
            GradeFactory factory,
            IProdutoRepositorio produtoRepo,
            IFornecedorRepositorio fornecedorRepo,
            IUnidadeEntregaRepositorio unidadeEntregaRepo,
            IAgendamentoRepositorio agendamentoRepositorio
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _produtoRepo = produtoRepo;
            _fornecedorRepo = fornecedorRepo;
            _unidadeRepo = unidadeEntregaRepo;
            _agendamentoRepo = agendamentoRepositorio;
        }

        public async Task<GradeResponse> CreateGrade
            (
                GradeCreateDto grade,
                CancellationToken cancellationToken = default
            )
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(grade, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var gradeCriada = await _factory.CreateGradeFromDto(grade, cancellationToken);

            var diasSolicitados = gradeCriada.DiasSemana.Split(',').Select(int.Parse).ToList();

            if (string.IsNullOrWhiteSpace(gradeCriada.DiasSemana))
            {
                throw new BusinessException("Dias da semana não informados.");
            }

            var temDiaValidoNoIntervalo = false;

            for (var date = gradeCriada.DataInicio; date <= gradeCriada.DataFim; date = date.AddDays(1))
            {
                if (diasSolicitados.Contains((int)date.DayOfWeek))
                {
                    temDiaValidoNoIntervalo = true;
                    break;
                }
            }

            if (!temDiaValidoNoIntervalo)
            {
                throw new BusinessException(
                    "O intervalo de datas informado não contém nenhum dos dias da semana selecionados. Nenhuma vaga seria gerada.");
            }

            await _repo.CreateGrade(gradeCriada, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            var slots = gradeCriada.GerarSlots();
            Console.WriteLine($"Slots gerados: {slots.Count}");

            if (slots.Count > 0)
            {
                await _agendamentoRepo.AddRangeAsync(slots, cancellationToken);
                await _repo.SaveChangesAsync(cancellationToken);
            }

            await _repo.SaveChangesAsync(cancellationToken);
            return MapToResponse(gradeCriada);
        }

        public async Task<List<GradeResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listarGrades = await _repo.GetAll(cancellationToken);

            if (listarGrades.Count == 0)
            {
                return [];
            }

            return listarGrades.Select(MapToResponse).ToList();
        }

        public async Task<GradeResponse> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken) ??
                throw new NotFoundException("Grade não encontrado");

            return MapToResponse(gradeEncontrada);
        }

        public async Task<GradeResponse> UpdateGrade(
            Guid id,
            GradeUpdateDto grade,
            CancellationToken cancellationToken = default)
        {
            var produto = await _produtoRepo.GetById(grade.ProdutoId, cancellationToken)
                ?? throw new NotFoundException("Produto não encontrado");

            var fornecedor = await _fornecedorRepo.GetById(grade.FornecedorId, cancellationToken)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            ValidationResult validationResult = await _updateValidator.ValidateAsync(grade, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var gradeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Grade não encontrado");

            gradeEncontrada.Produto = produto;
            gradeEncontrada.Fornecedor = fornecedor;
            gradeEncontrada.ProdutoId = produto.Id;
            gradeEncontrada.FornecedorId = fornecedor.Id;
            gradeEncontrada.DataInicio = grade.DataInicio;
            gradeEncontrada.DataFim = grade.DataFim;
            gradeEncontrada.HoraInicial = grade.HoraInicial;
            gradeEncontrada.HoraFinal = grade.HoraFinal;
            gradeEncontrada.IntervaloMinutos = grade.IntervaloMinutos;
            gradeEncontrada.UpdatedAt = DateTime.UtcNow;

            var gradeAtualizada = await _repo.Update(gradeEncontrada, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            return MapToResponse(gradeAtualizada);
        }

        public async Task DeleteGrade(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Grade não encontrado");

            var existeAgendamentoBloqueante = await _agendamentoRepo.ExisteAgendamentoBloqueantePorGrade(id, cancellationToken);

            if (existeAgendamentoBloqueante)
            {
                throw new BusinessException(
                    "Não é possível remover a grade pois existem agendamentos ativos, em andamento ou finalizados.");
            }

            await _repo.Delete(gradeEncontrada, cancellationToken);
        }

        private static GradeResponse MapToResponse(Grade g) =>

            new GradeResponse
            {
                Id = g.Id,
                Produto = g.Produto.Nome,
                Fornecedor = g.Fornecedor.Nome,
                ProdutoId = g.ProdutoId,
                FornecedorId = g.FornecedorId,
                DataInicio = g.DataInicio,
                DataFim = g.DataFim,
                HoraInicial = g.HoraInicial,
                HoraFinal = g.HoraFinal,
                IntervaloMinutos = g.IntervaloMinutos,
                DiasSemana = g.DiasSemana,
                UnidadeEntrega = g.UnidadeEntrega.Nome,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
    }
}
