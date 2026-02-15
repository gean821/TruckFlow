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
        private readonly ILocalDescargaRepositorio _descargaRepositorio;
        private readonly IEmpresaRepositorio _empresaRepo;

        public GradeFactory(
            IProdutoRepositorio repo,
            IFornecedorRepositorio fornecedorRepositorio,
            ILocalDescargaRepositorio descargaRepositorio,
            IEmpresaRepositorio empresaRepo
            )
        {
            _repo = repo;
            _fornecedorRepositorio = fornecedorRepositorio;
            _descargaRepositorio = descargaRepositorio;
            _empresaRepo = empresaRepo;
        }

        public async Task<Grade> CreateGradeFromDto(GradeCreateDto dto, CancellationToken token = default)
        {
            var Produto = await _repo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("Não foi possível encontrar o produto.");

            var Fornecedor = await _fornecedorRepositorio.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Não foi possível encontrar o fornecedor.");

            var unidade = await _descargaRepositorio.GetById(
                dto.LocalDescargaId,
                token);
            //?? throw new NotFoundException("Não foi encontrada a o local de descarga para a grade.");

            var empresa = await _empresaRepo.GetById(dto.EmpresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

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
                LocalDescarga = unidade,
                LocalDescargaId = dto.LocalDescargaId,
                IntervaloMinutos = dto.IntervaloMinutos,
                Empresa = empresa,
                DiasSemana = dto.DiasSemana,
            };
        }
    }
}
