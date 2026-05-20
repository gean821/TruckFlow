using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IProdutoFornecedorRepositorio
    {
        Task<ProdutoFornecedor?> GetByFornecedorAndCodigo(
            Guid fornecedorId,
            string codigoFornecedor,
            CancellationToken token = default);

        Task<ProdutoFornecedor?> GetByProdutoAndFornecedor(
            Guid produtoId,
            Guid fornecedorId,
            CancellationToken token = default);

        /// <summary>
        /// Auto-learning: cria/atualiza mapping fornecedor→produto com código (e EAN se disponível).
        /// Chamado quando admin confirma manualmente um item PendenteRevisao.
        /// </summary>
        Task UpsertMapping(
            Guid empresaId,
            Guid fornecedorId,
            Guid produtoId,
            string? codigoFornecedor,
            string? eanFornecedor,
            CancellationToken token = default);

        Task SaveChangesAsync(CancellationToken token = default);
    }
}