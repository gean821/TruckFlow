using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class ItemPlanejamento : EntidadeBase, IEmpresaScoped
    {
        public required Produto Produto { get; set; }
        public required Guid ProdutoId { get; set; }
        public required PlanejamentoRecebimento PlanejamentoRecebimento { get; set; }
        public required Guid PlanejamentoRecebimentoId { get; set; }
        public required decimal QuantidadeTotalPlanejada { get; set; }
        public required decimal CadenciaDiariaPlanejada { get; set; }
        public decimal QuantidadeTotalRecebida { get; set; }

        public ICollection<RecebimentoEvento>? RecebimentoEventos { get; set; } = [];

        public Guid EmpresaId { get; set; }
        public required Empresa Empresa { get; set; }

        public void RegistrarRecebimento(decimal quantidade)
        {
            if (quantidade <= 0)
                throw new Exception("Quantidade inválida.");

            QuantidadeTotalRecebida += quantidade;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool EstaConcluido()
            => QuantidadeTotalRecebida >= QuantidadeTotalPlanejada;
    }
}
