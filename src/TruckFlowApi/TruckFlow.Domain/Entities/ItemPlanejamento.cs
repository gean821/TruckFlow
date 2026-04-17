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

        public string DiasSemana { get; set; } = string.Empty;
        public decimal ToleranciaExtra { get; set; } = 30m;

        public ICollection<RecebimentoEvento>? RecebimentoEventos { get; set; } = [];

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public void RegistrarRecebimento(decimal quantidade)
        {
            if (quantidade <= 0)
                throw new Exception("Quantidade inválida.");

            QuantidadeTotalRecebida += quantidade;
            UpdatedAt = DateTime.UtcNow;
        }

        public void EstornarRecebimento(decimal quantidade)
        {
            if (quantidade <= 0)
                throw new Exception("Quantidade inválida.");

            QuantidadeTotalRecebida = Math.Max(0m, QuantidadeTotalRecebida - quantidade);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool EstaConcluido()
            => QuantidadeTotalRecebida >= QuantidadeTotalPlanejada;

        public IReadOnlyCollection<DayOfWeek> DiasSemanaEnum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DiasSemana))
                    return Array.Empty<DayOfWeek>();

                return DiasSemana
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (DayOfWeek)int.Parse(x))
                    .Distinct()
                    .ToList();
            }
        }

        public int QuantidadeDiasOperacao => DiasSemanaEnum.Count;

        public bool OperaEm(DateTime data)
            => DiasSemanaEnum.Contains(data.DayOfWeek);

        public decimal QuantidadeRecebidaNoDia(DateTime data)
        {
            if (RecebimentoEventos is null)
                return 0m;

            return RecebimentoEventos
                .Where(e => e.DataRecebimento.Date == data.Date)
                .Sum(e => e.Quantidade);
        }

        public bool MetaDiariaAtingida(DateTime data)
        {
            if (!OperaEm(data))
                return false;

            return QuantidadeRecebidaNoDia(data) >= CadenciaDiariaPlanejada + ToleranciaExtra;
        }
    }
}
