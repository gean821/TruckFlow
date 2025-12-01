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
        public Task<Grade> CreateGradeFromDto
            (GradeCreateDto dto,
            List<Produto>? produtos = null,
            List<Fornecedor>? fornecedores = null,
            CancellationToken token = default
            )
        {
            ArgumentNullException.ThrowIfNull(dto);

            var Grade = new Grade
            {
                Produtos = produtos?? new List<Produto>(),
                Fornecedores = fornecedores?? new List<Fornecedor>(),
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                HoraInicial = dto.HoraInicial,
                HoraFinal = dto.HoraFinal,
                IntervaloMinutos = dto.IntervaloMinutos,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = dto.UpdatedAt,
                DeletedAt= dto.DeletedAt
            };

            return Task.FromResult(Grade);
        }
    }
}
