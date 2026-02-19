using NFe.Classes.Informacoes.Total;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class ProdutoFactory(ILocalDescargaRepositorio repo)
    {
        private readonly ILocalDescargaRepositorio _repo = repo;

        public async Task<Produto> CreateProdutoFromDto(
            ProdutoCreateDto dto,
            Guid empresaId,
            CancellationToken token = default)
        {
            var localDescarga = await _repo.GetById(dto.LocalDescargaId, token)
                ?? throw new NotFoundException("Local de descarga não encontrado.");

            return new Produto
            {
                Nome = dto.Nome,
                LocalDescarga = localDescarga,
                LocalDescargaId = localDescarga.Id,
                EmpresaId = empresaId,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
