using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Fornecedor;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class FornecedorFactory
    {
        private readonly IProdutoRepositorio _repo;

        public FornecedorFactory(IProdutoRepositorio repo) => _repo = repo;

        public async Task<Fornecedor> CreateFornecedorFromDto(FornecedorCreateDto dto, CancellationToken token = default)
        {
            var produto = await _repo.GetById(dto.ProdutoId, token)
                ?? throw new ArgumentNullException("Não foi possível encontrar o produto.");

            return new Fornecedor
            {
                Nome = dto.Nome,
                Produto = produto,
                ProdutoId = produto.Id
            };
        }
    }
}
