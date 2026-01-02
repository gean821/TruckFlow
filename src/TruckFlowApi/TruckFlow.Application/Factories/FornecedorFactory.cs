using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class FornecedorFactory
    {
        private readonly IProdutoRepositorio _repo;

        public FornecedorFactory(IProdutoRepositorio repo) => _repo = repo;

        public Task<Fornecedor> CreateFornecedorFromDto
            (
                FornecedorCreateDto dto,
                List<Produto>? produtos = null,
                CancellationToken token = default
            )
        {
            ArgumentNullException.ThrowIfNull(dto);

            var fornecedor = new Fornecedor
            {
                Nome = dto.Nome,
                Cnpj = dto.Cnpj,
                Produtos = produtos?? new List<Produto>(),
                CreatedAt = DateTime.UtcNow,
            };

            return Task.FromResult(fornecedor);
        }
    }
}
