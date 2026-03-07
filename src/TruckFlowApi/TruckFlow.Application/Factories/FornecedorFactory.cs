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
        public Task<Fornecedor> CreateFornecedorFromDto(
            FornecedorCreateDto dto,
            Guid empresaId,
            List<Produto>? produtos = null)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var fornecedor = new Fornecedor
            {
                Nome = dto.Nome,
                Cnpj = dto.Cnpj,
                Produtos = produtos ?? [],
                EmpresaId = empresaId,
                CreatedAt = DateTime.UtcNow
            };

            return Task.FromResult(fornecedor);
        }
    }
}

