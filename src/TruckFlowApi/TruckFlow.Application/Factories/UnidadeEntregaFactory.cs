using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.UnidadeEntrega;
namespace TruckFlow.Application.Factories
{
    public class UnidadeEntregaFactory
    {
        public UnidadeEntrega CreateUnidadeEntregaFromDto(UnidadeEntregaCreateDto dto)
        {
            return new UnidadeEntrega
            {
                Nome = dto.Nome,
                Localizacao = dto.Localizacao,
            };
        }
    }
}
