using System;

namespace TruckFlow.Domain.Dto.LocalDescarga
{
    public class LocalDescargaListQueryDto
    {
        public bool? Ativa { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public string? Search { get; set; }
    }
}
