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
        private readonly GradeFactory _factory;

        public GradeService(
            IGradeRepositorio repo,
            IValidator<GradeCreateDto> createValidator,
            IValidator<GradeUpdateDto> updateValidator,
            GradeFactory factory,
            IProdutoRepositorio produtoRepo,
            IFornecedorRepositorio fornecedorRepo
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _produtoRepo = produtoRepo;
            _fornecedorRepo = fornecedorRepo;
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

            await  _repo.CreateGrade(gradeCriada, cancellationToken);
            await  _repo.SaveChangesAsync(cancellationToken);

            return MapToResponse(gradeCriada);
        }

        public async Task<GradeResponse> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken) ??
                throw new NotFoundException("Grade não encontrado"); // lembra da implemnetacao em Resources Geanzada

            return MapToResponse(gradeEncontrada);
        }

        public async Task DeleteGrade(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Grade não encontrado");

            await _repo.Delete(gradeEncontrada, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<GradeResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listarGrades = await _repo.GetAll(cancellationToken);

            if (listarGrades.Count == 0)
            {
                return [];
            }

            var listaGradesDto = listarGrades.Select(g => new GradeResponse
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
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();

            return listaGradesDto;
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

            return MapToResponse(gradeAtualizada);
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
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
    }
}
