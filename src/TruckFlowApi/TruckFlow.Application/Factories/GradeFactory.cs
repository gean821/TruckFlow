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
        public static Grade Create(
            GradeCreateDto dto,
            Guid empresaId,
            Produto produto,
            Fornecedor fornecedor,
            LocalDescarga localDescarga,
            UnidadeEntrega unidade
            )
        {
            return new Grade
            {
                Produto = produto,
                ProdutoId = produto.Id,
                Fornecedor = fornecedor,
                FornecedorId = fornecedor.Id,
                LocalDescarga = localDescarga,
                LocalDescargaId = localDescarga.Id,
                UnidadeEntrega = unidade,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                HoraInicial = dto.HoraInicial,
                HoraFinal = dto.HoraFinal,
                IntervaloMinutos = dto.IntervaloMinutos,
                EmpresaId = empresaId,
                CreatedAt = DateTime.UtcNow,
                DiasSemana = dto.DiasSemana
            };
        }
    }
}
