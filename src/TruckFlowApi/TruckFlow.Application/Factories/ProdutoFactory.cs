using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Produto;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class ProdutoFactory
    {
        private readonly ILocalDescargaRepositorio _repo;

        public ProdutoFactory(ILocalDescargaRepositorio repo) => _repo = repo;
        
        public async Task<Produto> CreateProdutoFromDto(ProdutoCreateDto dto, CancellationToken token = default)
        {
            var localDescarga = await _repo.GetById(dto.LocalDescargaId, token) 
                ?? throw new ArgumentNullException("Não foi possível encontrar o local de descarga.");
            
            return new Produto
            {
                Nome = dto.Nome,
                LocalDescarga = localDescarga
            };
        }

    }
}
