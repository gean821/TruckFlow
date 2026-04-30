using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TruckFlow.Application.Sefaz
{
    /// <summary>
    /// Mock determinístico do cliente SEFAZ para uso em dev/sem certificado.
    /// Convenção pra simular cenários por sufixo da chave (44 dígitos):
    ///   ...0001 → cStat 101 (cancelada)
    ///   ...0010 → cStat 110 (denegada)
    ///   ...0217 → cStat 217 (NF-e não consta na base)
    ///   qualquer outro → cStat 100 (autorizada)
    /// </summary>
    public class FakeSefazClient : ISefazClient
    {
        private readonly ILogger<FakeSefazClient> _logger;
        private readonly SefazOptions _options;

        public FakeSefazClient(ILogger<FakeSefazClient> logger, IOptions<SefazOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public Task<ConsultaProtocoloResultado> ConsultarProtocoloAsync(
            string chaveAcesso,
            string ufEmitente,
            CancellationToken token)
        {
            _logger.LogInformation(
                "[FakeSefazClient] Consulta protocolo simulada. Chave={Chave} UF={UF}",
                chaveAcesso, ufEmitente);

            var (cStat, xMotivo) = chaveAcesso switch
            {
                var c when c.EndsWith("0001") => (101, "Cancelamento de NF-e homologado"),
                var c when c.EndsWith("0010") => (110, "Uso Denegado"),
                var c when c.EndsWith("0217") => (217, "NF-e nao consta na base de dados da SEFAZ"),
                _ => (100, "Autorizado o uso da NF-e")
            };

            var resultado = new ConsultaProtocoloResultado
            {
                ChaveAcesso = chaveAcesso,
                CStat = cStat,
                XMotivo = xMotivo,
                Protocolo = cStat == 100 ? $"FAKE{DateTime.UtcNow:yyyyMMddHHmmssfff}" : null,
                DataAutorizacao = cStat == 100 ? DateTime.UtcNow : null,
                Ambiente = _options.Ambiente,
                RawRespostaXml = null
            };

            return Task.FromResult(resultado);
        }
    }
}
