namespace TruckFlow.Test.Application.NotaFiscal;

public static class NotaFiscalXmlFixtures
{
    public const string ChaveAutorizada = "35240112345678000199550010000012345123456789";
    public const string ChaveNaoDisponivel = "35240100000000000000550010000000010000000010";
    public const string ChaveDenegada = "35240100000000000000550010000000020000000020";

    public const string NfeProcAutorizada = """
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

    public const string NfeBareAutorizada = """
        <?xml version="1.0" encoding="UTF-8"?>
        <NFe xmlns="http://www.portalfiscal.inf.br/nfe">
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
              <modFrete>9</modFrete>
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
        """;

    public const string XmlInvalido = "<naoENota>lixo</naoENota>";
}
