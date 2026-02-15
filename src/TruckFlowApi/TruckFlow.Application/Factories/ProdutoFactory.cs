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
    public class ProdutoFactory
    {
        private readonly ILocalDescargaRepositorio _repo;
        private readonly IEmpresaRepositorio _empresaRepo;

        public ProdutoFactory(
            ILocalDescargaRepositorio repo,
            IEmpresaRepositorio empresaRepo)
        {
            _repo = repo;
            _empresaRepo = empresaRepo;
        }

        public async Task<Produto> CreateProdutoFromDto(ProdutoCreateDto dto, CancellationToken token = default)
        {
            var localDescarga = await _repo.GetById(dto.LocalDescargaId, token) 
                ?? throw new NotFoundException("Não foi possível encontrar o local de descarga.");

            var empresa = await _empresaRepo.GetById(dto.EmpresaId, token)
               ?? throw new NotFoundException("Empresa não encontrada.");

            return new Produto
            {
                Nome = dto.Nome,
                LocalDescarga = localDescarga,
                Empresa = empresa,
                EmpresaId = empresa.Id,
                LocalDescargaId = localDescarga.Id,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
