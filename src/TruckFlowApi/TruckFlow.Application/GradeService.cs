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
        private readonly ILocalDescargaRepositorio _localDescargaRepo;
        private readonly CurrentUserGuard _currentUser;

        public GradeService(
            IGradeRepositorio repo,
            IValidator<GradeCreateDto> createValidator,
            IValidator<GradeUpdateDto> updateValidator,
            IProdutoRepositorio produtoRepo,
            IFornecedorRepositorio fornecedorRepo,
            IUnidadeEntregaRepositorio unidadeEntregaRepo,
            IAgendamentoRepositorio agendamentoRepositorio,
            ILocalDescargaRepositorio descargaRepo,
            CurrentUserGuard guard
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _produtoRepo = produtoRepo;
            _fornecedorRepo = fornecedorRepo;
            _unidadeRepo = unidadeEntregaRepo;
            _agendamentoRepo = agendamentoRepositorio;
            _localDescargaRepo = descargaRepo;
            _currentUser = guard;
        }

        public async Task<GradeResponse> CreateGrade(
            GradeCreateDto dto,
            CancellationToken token = default)
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var empresaId = _currentUser.GetEmpresaId();

            var produto = await _produtoRepo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("Produto não encontrado");

            var fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            var descarga = await _localDescargaRepo.GetById(dto.LocalDescargaId, token)
                ?? throw new NotFoundException("Local de descarga não encontrado");

            var grade = GradeFactory.Create(
                dto,
                empresaId,
                produto,
                fornecedor,
                descarga,
                descarga.UnidadeEntrega
            );

            await _repo.CreateGrade(grade, token);

            var slots = grade.GerarSlots();

            if (slots.Count > 0)
                await _agendamentoRepo.AddRangeAsync(slots, token);

            await _repo.SaveChangesAsync(token);

            return MapToResponse(grade);
        }
        public async Task<List<GradeResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listarGrades = await _repo.GetAll(cancellationToken);
            return listarGrades.Select(MapToResponse).ToList();
        }

        public async Task<GradeResponse> GetById(
            Guid id,
            CancellationToken cancellationToken = default
            )
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken) ??
                throw new NotFoundException("Grade não encontrado");

            return MapToResponse(gradeEncontrada);
        }

        public async Task<GradeResponse> UpdateGrade(
            Guid id,
            GradeUpdateDto dto,
            CancellationToken token = default
        )
        {
            var validation = await _updateValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var grade = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Grade não encontrada");

            var existeBloqueante =
                await _agendamentoRepo.ExisteAgendamentoBloqueantePorGrade(id, token);

            if (existeBloqueante)
            {
                throw new BusinessException(
                    "Não é possível alterar a grade pois existem agendamentos ativos.");
            }

            ApplyPatch(grade, dto, token);

            await _repo.Update(grade, token);
            return MapToResponse(grade);
        }
        public async Task DeleteGrade(
            Guid id,
            CancellationToken cancellationToken = default
            )
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Grade não encontrado");

            var existeAgendamentoBloqueante = 
                await _agendamentoRepo.ExisteAgendamentoBloqueantePorGrade(id, cancellationToken);

            if (existeAgendamentoBloqueante)
            {
                throw new BusinessException(
                    "Não é possível remover a grade pois existem agendamentos ativos, em andamento ou finalizados.");
            }

            await _repo.Delete(gradeEncontrada, cancellationToken);
        }

        private async void ApplyPatch(
            Grade grade,
            GradeUpdateDto dto,
            CancellationToken token = default)
        {
            if (dto.ProdutoId is not null)
            {
                var produto = await _produtoRepo.GetById(dto.ProdutoId.Value, token)
                    ?? throw new NotFoundException("Produto não encontrado");

                grade.Produto = produto;
                grade.ProdutoId = produto.Id;
            }

            if (dto.FornecedorId is not null)
            {
                var fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId.Value, token)
                    ?? throw new NotFoundException("Fornecedor não encontrado");

                grade.Fornecedor = fornecedor;
                grade.FornecedorId = fornecedor.Id;
            }

            if (dto.LocalDescargaId is not null)
            {
                var descarga = await _localDescargaRepo.GetById(dto.LocalDescargaId.Value, token)
                    ?? throw new NotFoundException("Local não encontrado");

                grade.LocalDescarga = descarga;
                grade.LocalDescargaId = descarga.Id;
            }

            if (dto.DataInicio is not null)
                grade.DataInicio = dto.DataInicio.Value;

            if (dto.DataFim is not null)
                grade.DataFim = dto.DataFim.Value;

            if (dto.HoraInicial is not null)
                grade.HoraInicial = dto.HoraInicial.Value;

            if (dto.HoraFinal is not null)
                grade.HoraFinal = dto.HoraFinal.Value;

            if (dto.IntervaloMinutos is not null)
                grade.IntervaloMinutos = dto.IntervaloMinutos.Value;

            if (dto.DiasSemana is not null)
                grade.DiasSemana = dto.DiasSemana;

            grade.UpdatedAt = DateTime.UtcNow;
        }

        private static GradeResponse MapToResponse(Grade g) =>
            
            new GradeResponse
            {
                Id = g.Id,
                Produto = g.Produto.Nome,
                Fornecedor = g.Fornecedor.Nome,
                ProdutoId = g.ProdutoId,
                DataInicio = g.DataInicio,
                DataFim = g.DataFim,
                HoraInicial = g.HoraInicial,
                HoraFinal = g.HoraFinal,
                IntervaloMinutos = g.IntervaloMinutos,
                DiasSemana = g.DiasSemana,
                UnidadeEntrega = g.UnidadeEntrega.Nome.ToString(),
                LocalDescarga = g.LocalDescarga.Nome.ToString(),
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
    }
}
