using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;


namespace TruckFlow.Application.Factories
{
    public class GradeFactory
    {
        private readonly IProdutoRepositorio _repo;

        public GradeFactory(IProdutoRepositorio repo) => _repo = repo;

        private readonly IFornecedorRepositorio _rep;

        public GradeFactory(IFornecedorRepositorio repo) => _rep = repo;
        
        public async Task<Grade> CreateGradeFromDto(GradeCreateDto dto, CancellationToken token = default)
        {
            var Produto = await _repo.GetById(dto.ProdutoId, token)
                ?? throw new ArgumentNullException("Não foi possível encontrar o produto.");

            var Fornecedor = await _rep.GetById(dto.FornecedorId, token)
                ?? throw new ArgumentNullException("Não foi possível encontrar o fornecedor.");

            return new Grade
            {
                Produto = Produto,
                ProdutoId = Produto.Id,
                Fornecedor = Fornecedor,
                FornecedorId = Fornecedor.Id,
                CreatedAt = DateTime.UtcNow,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                HoraInicial = dto.HoraInicial,
                HoraFinal = dto.HoraFinal,
                IntervaloMinutos = dto.IntervaloMinutos,
            };
        }
    }
}
