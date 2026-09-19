using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFe.Servicos;
using NFe.Utils;
using System.Text;

namespace TruckFlow.Application.Sefaz
{
    /// <summary>
    /// Cliente real SEFAZ via Zeus.Net.NFe.
    /// Requer certificado A1 (.pfx + senha) configurado em Sefaz:Certificado.
    /// Mesmo em homologação (Ambiente=2) o handshake TLS exige A1 válido.
    /// </summary>
    public class ZeusSefazClient : ISefazClient
    {
        private readonly ILogger<ZeusSefazClient> _logger;
        private readonly SefazOptions _options;

        public ZeusSefazClient(ILogger<ZeusSefazClient> logger, IOptions<SefazOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public Task<ConsultaProtocoloResultado> ConsultarProtocoloAsync(
            string chaveAcesso,
            string ufEmitente,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_options.Certificado.Caminho))
            {
                throw new InvalidOperationException(
                    "Sefaz:Certificado:Caminho não configurado. " +
                    "Defina o caminho do .pfx (A1) ou habilite Sefaz:UseFake=true para dev.");
            }

            if (!Enum.TryParse<Estado>(ufEmitente, ignoreCase: true, out var estado))
            {
                throw new ArgumentException(
                    $"UF inválida para SEFAZ: '{ufEmitente}'. Use sigla de 2 letras (ex: SP, RJ).",
                    nameof(ufEmitente));
            }

            var ambiente = _options.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;

            return Task.Run(() => ExecutarConsulta(chaveAcesso, estado, ambiente), token);
        }

        private ConsultaProtocoloResultado ExecutarConsulta(
            string chaveAcesso,
            Estado estado,
            TipoAmbiente ambiente)
        {
            var config = new ConfiguracaoServico
            {
                tpAmb = ambiente,
                cUF = estado,
                VersaoNfeConsultaProtocolo = VersaoServico.Versao400
            };

            config.Certificado.Arquivo = _options.Certificado.Caminho!;
            config.Certificado.Senha = _options.Certificado.Senha ?? string.Empty;

            _logger.LogInformation(
                "[ZeusSefazClient] Consulta protocolo SEFAZ. Chave={Chave} UF={UF} Ambiente={Ambiente}",
                chaveAcesso, estado, ambiente);

            var servicos = new ServicosNFe(config);
            var retorno = servicos.NfeConsultaProtocolo(chaveAcesso);
            var resp = retorno.Retorno;

            int cStat = resp.cStat;
            string xMotivo = resp.xMotivo ?? string.Empty;
            string? nProt = null;
            DateTime? dhRecbto = null;

            if (resp.protNFe?.infProt != null)
            {
                cStat = resp.protNFe.infProt.cStat;
                xMotivo = resp.protNFe.infProt.xMotivo ?? xMotivo;
                nProt = resp.protNFe.infProt.nProt;
                dhRecbto = resp.protNFe.infProt.dhRecbto.UtcDateTime;
            }

            _logger.LogInformation(
                "[ZeusSefazClient] Resposta SEFAZ. Chave={Chave} cStat={CStat} xMotivo={XMotivo} nProt={NProt}",
                chaveAcesso, cStat, xMotivo, nProt);

            return new ConsultaProtocoloResultado
            {
                ChaveAcesso = chaveAcesso,
                CStat = cStat,
                XMotivo = xMotivo,
                Protocolo = nProt,
                DataAutorizacao = dhRecbto,
                Ambiente = (int)ambiente,
                RawRespostaXml = retorno.RetornoStr
            };
        }

        public Task<ConsultaDistribuicaoResultado> ConsultarDistribuicaoAsync(
            string chaveAcesso,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_options.Certificado.Caminho))
            {
                throw new InvalidOperationException(
                    "Sefaz:Certificado:Caminho não configurado. " +
                    "Defina o caminho do .pfx (A1) ou habilite Sefaz:UseFake=true para dev.");
            }

            if (string.IsNullOrWhiteSpace(_options.CnpjConsultante))
            {
                throw new InvalidOperationException(
                    "Sefaz:CnpjConsultante não configurado. NFeDistribuicaoDFe exige informar " +
                    "explicitamente o CNPJ de quem consulta (o do certificado configurado) — " +
                    "ver Docs/sefaz-certificado-consulta-nfe.md.");
            }

            return Task.Run(() => ExecutarConsultaDistribuicao(chaveAcesso), token);
        }

        private ConsultaDistribuicaoResultado ExecutarConsultaDistribuicao(string chaveAcesso)
        {
            var config = new ConfiguracaoServico
            {
                tpAmb = _options.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                VersaoNFeDistribuicaoDFe = VersaoServico.Versao100
            };

            config.Certificado.Arquivo = _options.Certificado.Caminho!;
            config.Certificado.Senha = _options.Certificado.Senha ?? string.Empty;

            _logger.LogInformation(
                "[ZeusSefazClient] Consulta distribuição SEFAZ. Chave={Chave} CnpjConsultante={Cnpj}",
                chaveAcesso, _options.CnpjConsultante);

            var servicos = new ServicosNFe(config);
            
            var retorno = servicos.NfeDistDFeInteresse(
                "AN", _options.CnpjConsultante!, "0", "0", chaveAcesso);

            var resp = retorno.Retorno;
            var lote = resp.loteDistDFeInt?.FirstOrDefault();

            string? xmlNfe = ExtrairXmlDoLote(lote);

            _logger.LogInformation(
                "[ZeusSefazClient] Resposta distribuição SEFAZ. Chave={Chave} cStat={CStat} xMotivo={XMotivo} xmlDisponivel={Disponivel}",
                chaveAcesso, resp.cStat, resp.xMotivo, xmlNfe != null);

            return new ConsultaDistribuicaoResultado
            {
                ChaveAcesso = chaveAcesso,
                CStat = resp.cStat,
                XMotivo = resp.xMotivo ?? string.Empty,
                XmlNfe = xmlNfe
            };
        }


        private static string? ExtrairXmlDoLote(NFe.Classes.Servicos.DistribuicaoDFe.loteDistDFeInt? lote)
        {
            if (lote == null)
            {
                return null;
            }

            if (lote.NfeProc?.NFe?.infNFe != null)
            {
                return FuncoesXml.ClasseParaXmlString(lote.NfeProc);
            }

            if (lote.XmlNfe is { Length: > 0 })
            {
                try
                {
                    using var comprimido = new MemoryStream(lote.XmlNfe);
                    using var gzip = new System.IO.Compression.GZipStream(comprimido, System.IO.Compression.CompressionMode.Decompress);
                    using var descomprimido = new MemoryStream();
                    gzip.CopyTo(descomprimido);
                    return Encoding.UTF8.GetString(descomprimido.ToArray());
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
