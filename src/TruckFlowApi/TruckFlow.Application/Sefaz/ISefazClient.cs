namespace TruckFlow.Application.Sefaz
{
    public interface ISefazClient
    {
        Task<ConsultaProtocoloResultado> ConsultarProtocoloAsync(
            string chaveAcesso,
            string ufEmitente,
            CancellationToken token);
    }
}
