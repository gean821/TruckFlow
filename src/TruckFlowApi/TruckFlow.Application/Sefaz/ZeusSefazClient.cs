using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFe.Classes.Informacoes.Identificacao.Tipos;
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
            if (string.IsNullOrWhiteSpace(_options.Certificado.Thumbprint) && string.IsNullOrWhiteSpace(_options.Certificado.Caminho))
            {
                throw new InvalidOperationException(
                    "Certificado não configurado. Defina Sefaz:Certificado:Thumbprint (certificado " +
                    "instalado na máquina) ou Sefaz:Certificado:Caminho (.pfx), ou habilite Sefaz:UseFake=true para dev.");
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
                // ModeloDocumento/tpEmis não têm default utilizável (enums começam em valores != 0,
                // o default 0 do C# não bate com nenhum membro) — sem setar explicitamente, o
                // resolvedor de URL do webservice falha com "emissão tipo ," vazio (confirmado
                // com SEFAZ real, 2026-09-20).
                ModeloDocumento = ModeloDocumento.NFe,
                tpEmis = TipoEmissao.teNormal,
                // Sem isso a lib exige DiretorioSchemas (pasta com os XSD da NF-e) pra validar
                // o XML localmente antes de enviar — não vale empacotar XSDs só pra uma consulta
                // simples (chave de acesso); a SEFAZ valida do lado dela de qualquer forma.
                ValidarSchemas = false,
                // Webservices da SEFAZ exigem TLS 1.2 — sem forçar isso aqui o handshake falha
                // com "SSL connection could not be established" (confirmado com SEFAZ real, 2026-09-20).
                ProtocoloDeSeguranca = System.Net.SecurityProtocolType.Tls12,
                // ConfiguracaoServico.TimeOut é em MILISSEGUNDOS (vira HttpWebRequest.Timeout
                // direto, sem conversão) — 60 aqui vira 60ms, timeout instantâneo. Foi a causa
                // real dos "operation has timed out" (confirmado via decompilação do
                // RequestSefazDefault.SendRequest em DFe.Wsdl.dll, 2026-09-20).
                TimeOut = 60000,
                VersaoNfeConsultaProtocolo = VersaoServico.Versao400
            };

            ConfigurarCertificado(config);

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
            if (string.IsNullOrWhiteSpace(_options.Certificado.Thumbprint) && string.IsNullOrWhiteSpace(_options.Certificado.Caminho))
            {
                throw new InvalidOperationException(
                    "Certificado não configurado. Defina Sefaz:Certificado:Thumbprint (certificado " +
                    "instalado na máquina) ou Sefaz:Certificado:Caminho (.pfx), ou habilite Sefaz:UseFake=true para dev.");
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
                ModeloDocumento = ModeloDocumento.NFe,
                tpEmis = TipoEmissao.teNormal,
                ValidarSchemas = false,
                ProtocoloDeSeguranca = System.Net.SecurityProtocolType.Tls12,
                TimeOut = 60000,
                VersaoNFeDistribuicaoDFe = VersaoServico.Versao100
            };

            ConfigurarCertificado(config);

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


        /// <summary>
        /// Preferir Thumbprint (A1Repositorio, certificado instalado no Windows) quando
        /// configurado; Caminho (A1Arquivo, .pfx direto) fica como alternativa pra quem não
        /// quiser instalar o certificado na máquina — os dois modos foram validados contra a
        /// SEFAZ real (2026-09-20).
        ///
        /// A causa raiz de tudo que pareceu quebrado nesse caminho (timeout quase instantâneo,
        /// depois "SSL connection could not be established") era simples e não tinha nada a ver
        /// com TLS/certificado em si: <see cref="ConfiguracaoCertificado.KeyStorageFlags"/> não
        /// era setado (fica em DefaultKeySet=0), e o modo A1Arquivo usa exatamente essa flag pra
        /// carregar o .pfx (`CertificadoDigital.ObterDeArquivo`, decompilado de DFe.Utils.dll).
        /// Sem Exportable, o certificado carrega e reporta HasPrivateKey=true normalmente, mas a
        /// chave privada não fica utilizável pro handshake TLS de apresentação de certificado
        /// cliente — só descoberto comparando com um SslStream manual que funcionava, decompilando
        /// a lib (RequestSefazDefault/CertificadoDigital) e testando cada hipótese isoladamente.
        /// </summary>
        private void ConfigurarCertificado(ConfiguracaoServico config)
        {
            // ConfiguracaoServico.ProtocoloDeSeguranca nem sempre é suficiente sozinho — issue
            // conhecido da lib (ZeusAutomacao/DFe.NET #519, "could not create ssl/tls secure
            // channel", marcado resolvido) precisou forçar isso também globalmente via
            // ServicePointManager. Barato e idempotente, mantido aqui.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            if (!string.IsNullOrWhiteSpace(_options.Certificado.Thumbprint))
            {
                config.Certificado.TipoCertificado = TipoCertificado.A1Repositorio;
                config.Certificado.Serial = _options.Certificado.Thumbprint;
                return;
            }

            config.Certificado.TipoCertificado = TipoCertificado.A1Arquivo;
            config.Certificado.Arquivo = _options.Certificado.Caminho!;
            config.Certificado.Senha = _options.Certificado.Senha ?? string.Empty;
            // A causa raiz real do "SSL connection could not be established" — ver doc da
            // classe acima. Sem isso o handshake TLS falha silenciosamente mesmo com o
            // certificado e a senha corretos.
            config.Certificado.KeyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable;
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
