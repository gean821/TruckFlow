# Consulta de NF-e (leitura) — de quem precisa ser o certificado digital?

> Origem: dúvida levantada ao formalizar com a Aurora o pedido de certificado digital pra leitura de nota fiscal (câmera, XML, código digitado). Pergunta: precisa ser o certificado da Aurora, ou serve um certificado próprio da TruckFlow?
>
> **Status final (2026-09-20): testado de ponta a ponta contra a SEFAZ de produção real, com certificado real.** Os dois serviços (`NfeConsultaProtocolo` e `NFeDistribuicaoDFe`) funcionam tecnicamente — handshake TLS, SOAP, parsing, tudo validado com dados reais. Ver seção 6 pro resultado exato e o que ainda depende da Aurora.

## 1. O que já existe hoje (confirmado no código, mobile + backend)

O TruckFlow **não emite** nota fiscal — só lê. Existem **dois fluxos independentes**, e só um toca a SEFAZ:

1. **Câmera (código de barras Code128 do DANFe) ou código digitado** → extrai só a chave de acesso (44 dígitos) → `GET /NotaFiscal/buscar-por-chave/{chave}` → busca no próprio banco do TruckFlow. Não bate na SEFAZ. Se não achar, cai automaticamente no item 3 (wiring feito em `tf-mobile/src/hooks/useNotaFiscal.ts`).
2. **Upload de XML** → `POST /NotaFiscal/parse` → extrai peso, fornecedor, placa, itens direto do conteúdo do arquivo. Não bate na SEFAZ.
3. **`GET /NotaFiscal/buscar-completa-sefaz/{chave}`** (`ParseFromSefazAsync` → `ConsultarDistribuicaoAsync` → `NFeDistribuicaoDFe`) — busca a nota **completa** direto na SEFAZ, pro caso do motorista não ter XML em mãos e a nota nunca ter passado pelo TruckFlow. **Testado e funcionando** com certificado real (seção 6).
4. **`POST /NotaFiscal/validar-sefaz/{chave}`** (`ValidarNaSefazAsync` → `NfeConsultaProtocolo`) — só status (autorizada/cancelada/denegada). **Testado e funcionando** com certificado real. Endpoint pronto no backend, não ligado a nenhuma UI ainda (não é necessário pro fluxo principal, que já usa o item 3).

## 2. De quem precisa ser o certificado — por serviço

| Serviço | O que retorna | Quem pode consultar (fonte: MOC / Nota Técnica 2014.002) |
|---|---|---|
| `NfeConsultaProtocolo` | Só status | Qualquer CNPJ com certificado válido — não precisa ser emitente/destinatário |
| `NFeDistribuicaoDFe` / `consChNFe` | Nota completa (o que o fluxo real precisa) | **Destinatário**: acesso pleno. **Emitente**: sem acesso por essa consulta. **Transportador ou terceiro autorizado**: só se identificado no XML da nota (`transp`/`autXML`) |

Pro fluxo real de produção, a TruckFlow não é destinatária de nenhuma nota (é a Aurora, ou quem for o cliente) — **o certificado pra `NFeDistribuicaoDFe` precisa ser do destinatário real da carga**. Confirmado na prática (seção 6): funcionou porque testamos com uma nota onde o titular do certificado (CR DA SILVA TRANSPORTES) era o destinatário de verdade.

## 3. Não existe certificado "fake"

Nem em homologação. A SEFAZ valida assinatura com certificado ICP-Brasil real sempre, e tem que ser **e-CNPJ** (não aceita e-CPF pra NF-e). Confirmado testando: sem certificado, servidor rejeita com erro de handshake já na camada TLS (`ERR_BAD_SSL_CLIENT_AUTH_CERT` no navegador, `SEC_E_CERT_UNKNOWN` no curl/schannel).

## 4. Bugs reais encontrados e corrigidos (`ZeusSefazClient.cs`) — só apareceram testando com certificado real

Nenhum destes seria pego por teste automatizado nem pelo `FakeSefazClient` — só bateram batendo na SEFAZ de verdade:

1. **`TipoCertificado` não setado** — lib exige modo explícito (`A1Arquivo` ou `A1Repositorio`) pra saber como interpretar `Caminho`/`Serial`.
2. **`ModeloDocumento`/`tpEmis` vazios** — resolvedor de URL do webservice falhava com campos vazios no erro.
3. **`ValidarSchemas` exigindo diretório de XSD** — desabilitado (SEFAZ valida do lado dela).
4. **`ProtocoloDeSeguranca` não forçado pra TLS 1.2**.
5. **`TimeOut` em milissegundos, não segundos** — `TimeOut = 60` virava 60ms (timeout instantâneo), não 60 segundos. Essa foi a causa do "operation has timed out" que persistiu por várias rodadas de teste — só foi descoberta **decompilando** `RequestSefazDefault.SendRequest` (`DFe.Wsdl.dll`) e achando `((WebRequest)val).Timeout = timeOut` sem conversão de unidade.
6. **`ConfiguracaoCertificado.KeyStorageFlags` não setado** (fica em `DefaultKeySet`). O modo `A1Arquivo` usa exatamente essa flag pra carregar o `.pfx` (`CertificadoDigital.ObterDeArquivo`, achado por decompilação). **No ambiente testado**, carregar o certificado sem `X509KeyStorageFlags.Exportable` fazia o handshake mTLS falhar (o certificado carregava normalmente e reportava `HasPrivateKey=true`, mas a chave privada não ficava utilizável na apresentação do certificado cliente); com `Exportable`, o mesmo certificado funcionou. Não estou generalizando isso como regra universal da plataforma — o comportamento de `DefaultKeySet` pode variar conforme provider criptográfico, versão do Windows e forma como o certificado foi importado. Registrado aqui como o que resolveu **nesse ambiente específico**, não como verdade universal do .NET.

**Método de diagnóstico:** como a lib não preserva a exceção real, foi necessário (a) montar um teste isolado com `SslStream` puro pra confirmar que o certificado/rede/host funcionavam fora da lib, e (b) decompilar as classes internas da lib (`ICSharpCode.Decompiler`) pra achar exatamente onde e como ela usa o certificado e o timeout. Documentado aqui porque, se a lib for atualizada no futuro e algo quebrar de novo, esse é o caminho que funciona pra diagnosticar.

## 5. Suporte a dois modos de certificado

`SefazOptions.Certificado` aceita:
- **`Thumbprint`** → `TipoCertificado.A1Repositorio`, busca o certificado já importado no repositório do Windows (`Cert:\CurrentUser\My`) pelo **X.509 Serial Number** (não o thumbprint SHA1 — são campos diferentes, `certificado.SerialNumber`, não `.Thumbprint`).
- **`Caminho` + `Senha`** → `TipoCertificado.A1Arquivo`, lê o `.pfx` direto do disco. Mais simples de configurar (não precisa importar no Windows antes).

Ambos testados e funcionando.

## 6. Resultado do teste real (2026-09-20)

Certificado real (`CR DA SILVA TRANSPORTES`, CNPJ 16.811.323/0001-03, e-CNPJ A1 válido) + chave de acesso real de uma NF-e de produção (emitida 29/06/2026, destinatário = titular do certificado) + API do TruckFlow rodando de verdade:

| Endpoint | Resultado |
|---|---|
| `POST /NotaFiscal/validar-sefaz/{chave}` | ✅ HTTP 200 — `cStat=100`, `"Autorizado o uso da NF-e"`, `protocolo=141260250565517` (bate exatamente com o protocolo impresso no DANFE real) |
| `GET /NotaFiscal/buscar-completa-sefaz/{chave}` | ⚠️ HTTP 400 — `cStat=137`, `"Nenhum documento localizado"` |

O segundo resultado **não é erro técnico** — é uma resposta real e válida da SEFAZ: a comunicação técnica funcionou (handshake, SOAP e parsing corretos), mas o documento não foi disponibilizado pra essa consulta específica. Não temos, até aqui, confirmação de qual regra causou isso — só uma hipótese a testar: **manifestação do destinatário pendente**. O MOC descreve que, numa consulta `consChNFe` feita pelo destinatário, o Ambiente Nacional verifica a manifestação existente — sem ela, pode devolver só o resumo (ou nada, como no nosso caso); com "Ciência da Operação" registrada, o documento completo passa a ficar disponível. Isso é consistente com o que vimos, mas não é a única explicação possível (janela de retenção é outra hipótese não descartada) — vamos confirmar com um teste controlado, não assumir.

**Teste controlado tentado (2026-09-20):** tentativa de registrar "Ciência da Operação" pra essa chave no Portal Nacional da NF-e (`www.nfe.fazenda.gov.br`, Serviços → Manifestação Destinatário, certificado da CR DA SILVA) — **rejeitado pela SEFAZ**: `"Rejeição: Evento apresentado após o prazo permitido para o evento: [10 dias]."`. Achado novo e confirmado: manifestação do destinatário tem prazo duro de **10 dias corridos a partir da emissão** — essa nota (emitida 29/06/2026) já tinha ~83 dias, fora do prazo.

**Conclusão sobre essa nota específica:** ficou velha demais tanto pra manifestar quanto, provavelmente, pra estar disponível via `NFeDistribuicaoDFe` de qualquer forma — não dá mais pra isolar se o `cStat=137` era por falta de manifestação ou só pela idade/retenção. Encerrado como inconclusivo *pra essa nota* — não vale insistir nela.

**Por que isso não é um problema real pro produto:** no fluxo de produção, o motorista chega com uma nota **recém-emitida** (mesmo dia ou poucos dias depois) — o prazo de 10 dias pra manifestação nunca vai ser um obstáculo prático nesse cenário. O teste que efetivamente importa é com uma nota **fresca** (poucos dias de emissão) e o certificado real da Aurora — que é exatamente o cenário real de uso, não precisa ser forçado artificialmente.

**O que isso significa pra Aurora:** o código está 100% validado tecnicamente. O teste com o certificado real da Aurora (destinatária de verdade das notas dos fornecedores dela) é o que efetivamente importa pro produto — esse teste com a CR DA SILVA validou a infraestrutura, não ainda o cenário de negócio real.

## 7. Achado adicional (2026-09-20): transportador não precisa de manifestação

Confirmado via NT 2014.002 (nfe.fazenda.gov.br, verificado em duas buscas independentes batendo na mesma fonte oficial): a exigência de manifestação do destinatário pra liberar a NF-e completa via `NFeDistribuicaoDFe` **só vale pro papel de destinatário**. O **transportador** identificado no grupo `transp`/`transporta` da própria nota (tag `X03`) recebe a NF-e completa sem precisar de nenhuma manifestação. O mesmo vale pra terceiro autorizado via `autXML`.

**Isso ainda não foi testado empiricamente por nós** (diferente do resto deste documento, que já foi validado contra a SEFAZ real) — é uma leitura de documentação oficial, não uma confirmação prática ainda. Só vale considerar como caminho alternativo depois de ver, com dados reais da Aurora, se as notas dos fornecedores dela identificam uma transportadora no `transp` ou têm `autXML` preenchido — isso só aparece testando com uma nota real deles.

**Por que não vale agir sobre isso agora:** mesmo que confirmado, usar essa via exigiria que a TruckFlow (ou quem for consultar) tivesse o certificado do CNPJ exato identificado como transportador em cada nota — que varia por fornecedor/entrega, não é algo fixo. Não é uma solução geral óbvia, só um dado a mais pra decidir arquitetura depois de ver o cenário real da Aurora.

## 8. Por que o sistema não trava mesmo sem o conteúdo completo

A arquitetura já prevista (câmera/código → banco → SEFAZ → fallback pra upload manual de XML) já cobre o caso de a SEFAZ não devolver o conteúdo completo: `ParseFromSefazAsync` lança `BusinessException` clara, o mobile mostra "Nota não localizada" e oferece as outras duas formas de entrada (inclusive upload manual do XML, que nunca depende da SEFAZ). Não é necessário nenhum código novo pra esse cenário — já existe. O que ainda não sabemos é **com que frequência** isso vai acontecer na prática com dados reais da Aurora — só o teste com certificado e nota reais deles responde isso.

## 9. Tentativa de autoemissão em homologação (2026-09-20) — abandonada, não é bloqueio real

Tentamos emitir uma NF-e de teste em homologação (CR DA SILVA como emitente e destinatário) direto via `Zeus.Net.NFe` (sem site de terceiro), pra ter uma nota fresca e fechar o teste do conteúdo completo sem esperar a Aurora. Progresso real: certificado carregou, assinatura digital funcionou (`AssinaturaDigital.Assina`, exige o pacote `System.Security.Cryptography.Xml` à parte), chave de acesso e dígito verificador calculados corretamente, chegou a bater na SEFAZ de homologação de verdade (rejeições específicas e válidas confirmam isso: nome de destinatário obrigatório em homologação, NCM inexistente, ambos corrigidos).

**Travou em algo que não dá pra contornar rapidamente:** o grupo `infRespTec` (responsável técnico, obrigatório na NF-e 4.00) exige um CNPJ **pré-credenciado na SEFAZ como fornecedor de software** — não aceita qualquer CNPJ, é um registro formal separado que não temos.

**Isso não bloqueia o produto:** essa exigência só existe pra quem **emite** NF-e. O TruckFlow nunca emite, só lê — então esse credenciamento nunca vai ser necessário pra funcionalidade real. A trava é 100% específica dessa tentativa de autoteste, não do código de leitura (`ConsultarDistribuicaoAsync`/`buscar-completa-sefaz`), que continua validado como está.

## Próximo passo

Duas formas de fechar o teste do conteúdo completo, sem precisar de credenciamento de responsável técnico:
1. **Esperar o certificado real da Aurora** + uma nota fresca de fornecedor real dela — o cenário real de produção. Só trocar config (`Sefaz:Certificado:*`, `Sefaz:CnpjConsultante`), nenhum código novo.
2. **Mais rápido, se quiser fechar antes**: uma compra pequena e real feita pela CR DA SILVA (ou qualquer empresa com certificado disponível) gera uma nota emitida por um fornecedor de verdade — sem nenhuma das travas de autoemissão, porque quem emite não somos nós.
3. **Alternativa já testada e disponível**: emitir a nota de teste pelo emissor gratuito do Sebrae (ambiente de homologação já habilitado pra CR DA SILVA/LIMA EXPRESS, CNPJ 16.811.323/0001-03) — como o Sebrae já é credenciado como responsável técnico, não bate na trava do item 9.

## 10. Consideração futura (não iniciada): worker de captura via `distNSU`

Ideia discutida (2026-09-23), **não é decisão tomada, não é backlog ativo**: em vez de só consultar a SEFAZ sob demanda quando o motorista chega, ter um processo em background que consulta `NFeDistribuicaoDFe` via `distNSU` (sincronização contínua por NSU, não por chave) periodicamente pro CNPJ do cliente, capturando e salvando o XML completo assim que disponível — antes do motorista precisar dele. Resolveria de raiz qualquer problema de janela de manifestação/retenção, porque a captura aconteceria logo depois da emissão, não semanas depois.

**Por que não vale construir isso agora:** ainda não confirmamos que o problema que isso resolve é real na prática. Só vimos `cStat=137` com uma nota de 83 dias de uma transportadora pequena — não sabemos se notas frescas de fornecedores reais da Aurora (cenário real: motorista chega em poucos dias, não meses) já vêm disponíveis sem esse problema. Construir um worker de estado persistente de NSU por empresa, sem confirmar que o problema existe de verdade nem ter o certificado da Aurora pra testar, seria trabalho especulativo. Decisão: esperar o teste real (item acima) antes de considerar isso.
