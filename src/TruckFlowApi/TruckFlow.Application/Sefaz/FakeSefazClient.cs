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

        public Task<ConsultaDistribuicaoResultado> ConsultarDistribuicaoAsync(
            string chaveAcesso,
            CancellationToken token)
        {
            _logger.LogInformation(
                "[FakeSefazClient] Consulta distribuição simulada. Chave={Chave}",
                chaveAcesso);

            if (chaveAcesso.EndsWith("0010"))
            {
                return Task.FromResult(new ConsultaDistribuicaoResultado
                {
                    ChaveAcesso = chaveAcesso,
                    CStat = 137,
                    XMotivo = "Nenhum documento localizado",
                    XmlNfe = null
                });
            }

            if (chaveAcesso.EndsWith("0020"))
            {
                return Task.FromResult(new ConsultaDistribuicaoResultado
                {
                    ChaveAcesso = chaveAcesso,
                    CStat = 110,
                    XMotivo = "Uso Denegado",
                    XmlNfe = null
                });
            }

            return Task.FromResult(new ConsultaDistribuicaoResultado
            {
                ChaveAcesso = chaveAcesso,
                CStat = 138,
                XMotivo = "Documento localizado",
                XmlNfe = FixtureNfeProcAutorizada
            });
        }
        
        private const string FixtureNfeProcAutorizada = """
            <?xml version="1.0" encoding="UTF-8"?>
            <nfeProc versao="4.00" xmlns="http://www.portalfiscal.inf.br/nfe">
              <NFe>
                <infNFe versao="4.00" Id="NFe35240112345678000199550010000012345123456789">
                  <ide>
                    <cUF>35</cUF>
                    <cNF>12345678</cNF>
                    <natOp>Venda</natOp>
                    <mod>55</mod>
                    <serie>1</serie>
                    <nNF>12345</nNF>
                    <dhEmi>2026-09-10T08:30:00-03:00</dhEmi>
                    <tpNF>1</tpNF>
                    <idDest>2</idDest>
                    <cMunFG>3550308</cMunFG>
                    <tpImp>1</tpImp>
                    <tpEmis>1</tpEmis>
                    <cDV>9</cDV>
                    <tpAmb>2</tpAmb>
                    <finNFe>1</finNFe>
                    <indFinal>0</indFinal>
                    <indPres>0</indPres>
                    <procEmi>0</procEmi>
                    <verProc>TruckFlowTeste 1.0</verProc>
                  </ide>
                  <emit>
                    <CNPJ>11222333000181</CNPJ>
                    <xNome>Fornecedor Teste LTDA</xNome>
                    <enderEmit>
                      <xLgr>Rua Teste</xLgr>
                      <nro>100</nro>
                      <xBairro>Centro</xBairro>
                      <cMun>3550308</cMun>
                      <xMun>Sao Paulo</xMun>
                      <UF>SP</UF>
                      <CEP>01000000</CEP>
                      <cPais>1058</cPais>
                      <xPais>Brasil</xPais>
                    </enderEmit>
                    <IE>123456789</IE>
                    <CRT>3</CRT>
                  </emit>
                  <dest>
                    <CNPJ>99888777000166</CNPJ>
                    <xNome>Aurora Alimentos Teste</xNome>
                    <enderDest>
                      <xLgr>Rodovia Teste</xLgr>
                      <nro>500</nro>
                      <xBairro>Distrito Industrial</xBairro>
                      <cMun>4110706</cMun>
                      <xMun>Mandaguari</xMun>
                      <UF>PR</UF>
                      <CEP>86660000</CEP>
                      <cPais>1058</cPais>
                      <xPais>Brasil</xPais>
                    </enderDest>
                    <indIEDest>1</indIEDest>
                    <IE>987654321</IE>
                  </dest>
                  <det nItem="1">
                    <prod>
                      <cProd>PROD001</cProd>
                      <cEAN>7891000100103</cEAN>
                      <xProd>Milho em Grao</xProd>
                      <NCM>10059010</NCM>
                      <CFOP>5102</CFOP>
                      <uCom>KG</uCom>
                      <qCom>1000.0000</qCom>
                      <vUnCom>1.5000000000</vUnCom>
                      <vProd>1500.00</vProd>
                      <cEANTrib>7891000100103</cEANTrib>
                      <uTrib>KG</uTrib>
                      <qTrib>1000.0000</qTrib>
                      <vUnTrib>1.5000000000</vUnTrib>
                      <indTot>1</indTot>
                    </prod>
                    <imposto>
                      <ICMS>
                        <ICMS00>
                          <orig>0</orig>
                          <CST>00</CST>
                          <modBC>3</modBC>
                          <vBC>1500.00</vBC>
                          <pICMS>18.00</pICMS>
                          <vICMS>270.00</vICMS>
                        </ICMS00>
                      </ICMS>
                    </imposto>
                  </det>
                  <total>
                    <ICMSTot>
                      <vBC>1500.00</vBC>
                      <vICMS>270.00</vICMS>
                      <vICMSDeson>0.00</vICMSDeson>
                      <vFCP>0.00</vFCP>
                      <vBCST>0.00</vBCST>
                      <vST>0.00</vST>
                      <vFCPST>0.00</vFCPST>
                      <vFCPSTRet>0.00</vFCPSTRet>
                      <vProd>1500.00</vProd>
                      <vFrete>0.00</vFrete>
                      <vSeg>0.00</vSeg>
                      <vDesc>0.00</vDesc>
                      <vII>0.00</vII>
                      <vIPI>0.00</vIPI>
                      <vIPIDevol>0.00</vIPIDevol>
                      <vPIS>0.00</vPIS>
                      <vCOFINS>0.00</vCOFINS>
                      <vOutro>0.00</vOutro>
                      <vNF>1500.00</vNF>
                    </ICMSTot>
                  </total>
                  <transp>
                    <modFrete>1</modFrete>
                    <veicTransp>
                      <placa>ABC1D23</placa>
                      <UF>SP</UF>
                    </veicTransp>
                    <vol>
                      <qVol>20</qVol>
                      <esp>Sacas</esp>
                      <pesoL>980.000</pesoL>
                      <pesoB>1000.000</pesoB>
                    </vol>
                  </transp>
                  <pag>
                    <detPag>
                      <indPag>0</indPag>
                      <tPag>15</tPag>
                      <vPag>1500.00</vPag>
                    </detPag>
                  </pag>
                </infNFe>
              </NFe>
              <protNFe versao="4.00">
                <infProt>
                  <tpAmb>2</tpAmb>
                  <verAplic>SP_NFE_PL</verAplic>
                  <chNFe>35240112345678000199550010000012345123456789</chNFe>
                  <dhRecbto>2026-09-10T08:31:00-03:00</dhRecbto>
                  <nProt>135260000012345</nProt>
                  <digVal>abc123==</digVal>
                  <cStat>100</cStat>
                  <xMotivo>Autorizado o uso da NF-e</xMotivo>
                </infProt>
              </protNFe>
            </nfeProc>
            """;
    }
}
