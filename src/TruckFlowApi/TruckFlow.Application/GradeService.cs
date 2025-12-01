using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    internal class GradeService : IGradeService
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
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(grade, token);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            List<Produto> produtos = new();

            if (grade.ProdutoIds?.Count > 0)
            {
                produtos = await _produtoRepo.GetByIdsAsync(grade.ProdutoIds, token);
            }

            List<Fornecedor> fornecedores = new();

            if (grade.FornecedorIds?.Count > 0)
            {
                fornecedores = await _fornecedorRepo.GetByIdsAsync(grade.FornecedorIds, token);
            }
        }

    }
}
