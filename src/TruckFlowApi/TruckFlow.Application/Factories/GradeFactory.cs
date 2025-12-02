using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;


namespace TruckFlow.Application.Factories
{
    public class GradeFactory
    {
        private readonly IProdutoRepositorio _repo;
        private readonly IFornecedorRepositorio _fornecedorRepositorio;

        public GradeFactory(
            IProdutoRepositorio repo,
            IFornecedorRepositorio fornecedorRepositorio
            )
        {
            _repo = repo;
            _fornecedorRepositorio = fornecedorRepositorio;
        }

        public async Task<Grade> CreateGradeFromDto(GradeCreateDto dto, CancellationToken token = default)
        {
            var Produto = await _repo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("Não foi possível encontrar o produto.");

            var Fornecedor = await _fornecedorRepositorio.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Não foi possível encontrar o fornecedor.");

            return new Grade
            {
                Produto = Produto,
                ProdutoId = Produto.Id,
                Fornecedor = Fornecedor,
                FornecedorId = Fornecedor.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = dto.UpdatedAt,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                HoraInicial = dto.HoraInicial,
                HoraFinal = dto.HoraFinal,
                IntervaloMinutos = dto.IntervaloMinutos,
            };
        }
    }
}
