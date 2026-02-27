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
                DiasSemana = dto.DiasSemana,
                EmpresaId = empresaId,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
