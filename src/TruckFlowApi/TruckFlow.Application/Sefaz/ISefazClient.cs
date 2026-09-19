namespace TruckFlow.Application.Sefaz
{
    public interface ISefazClient
    {
        Task<ConsultaProtocoloResultado> ConsultarProtocoloAsync(
            string chaveAcesso,
            string ufEmitente,
            CancellationToken token);

        /// <summary>
        /// Busca a NF-e completa (não só status) via NFeDistribuicaoDFe/consChNFe.
        /// Diferente de ConsultarProtocoloAsync: não recebe UF do emitente, porque esse
        /// serviço é centralizado no Ambiente Nacional (AN), não roteado por UF.
        /// </summary>
        Task<ConsultaDistribuicaoResultado> ConsultarDistribuicaoAsync(
            string chaveAcesso,
            CancellationToken token);
    }
}
