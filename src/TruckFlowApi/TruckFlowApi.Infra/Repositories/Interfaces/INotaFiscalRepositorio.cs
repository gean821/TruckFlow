using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotaFiscalRepositorio
    {
        Task<NotaFiscal> SaveParsedNotaAsync(NotaFiscal nota, CancellationToken token);
        public Task SaveChangesAsync(CancellationToken token);

        Task<NotaFiscal?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token);

        /// <summary>
        /// Lookup por chave ignorando filter de tenant. Uso restrito a rotas de motorista
        /// (cross-tenant por design) que precisam resolver o EmpresaId da nota.
        /// </summary>
        Task<NotaFiscal?> ObterPorChaveAcrossTenantsAsync(string chaveAcesso, CancellationToken token);

        /// <summary>
        /// Histórico: retorna o ProdutoId da última vez que esse fornecedor enviou esse cProd
        /// numa NF com item já matchado (Status=Matched). Usado pra Pri 3 do matching.
        /// </summary>
        Task<Guid?> GetUltimoProdutoIdPorFornecedorECodigo(
            Guid fornecedorId,
            string codigoFornecedor,
            CancellationToken token);
    }
}
