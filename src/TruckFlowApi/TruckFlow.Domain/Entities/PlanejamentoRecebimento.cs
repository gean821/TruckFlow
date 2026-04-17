using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public sealed class PlanejamentoRecebimento : EntidadeBase, IEmpresaScoped
    {
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public StatusRecebimento StatusRecebimento { get; set; } = StatusRecebimento.Planejado;
        public ICollection<ItemPlanejamento> ItemPlanejamentos { get; set; } = [];

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }


        public void RecalcularStatus()
        {
            if (StatusRecebimento == StatusRecebimento.Encerrado)
                return;

            if (ItemPlanejamentos.All(i => i.EstaConcluido()))
                StatusRecebimento = StatusRecebimento.Concluido;
            else if (ItemPlanejamentos.Any(i => i.QuantidadeTotalRecebida > 0))
                StatusRecebimento = StatusRecebimento.EmAndamento;
            else
                StatusRecebimento = StatusRecebimento.Planejado;
        }

        public bool VigenciaContem(DateTime data)
            => data.Date >= DataInicio.Date && data.Date <= DataFim.Date;

        public ItemPlanejamento? ItemDoProduto(Guid produtoId)
            => ItemPlanejamentos.FirstOrDefault(i => i.ProdutoId == produtoId);

        public bool DeveCongelarProduto(Guid produtoId, DateTime dia)
        {
            var item = ItemDoProduto(produtoId);
            return item is not null && item.MetaDiariaAtingida(dia);
        }
    }
}
