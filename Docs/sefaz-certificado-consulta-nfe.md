# Consulta de NF-e (leitura) — de quem precisa ser o certificado digital?

> Origem: dúvida levantada ao formalizar com a Aurora o pedido de certificado digital pra leitura de nota fiscal (câmera, XML, código digitado). Pergunta: precisa ser o certificado da Aurora, ou serve um certificado próprio da TruckFlow?
>
> **Correção (2026-09-19):** a primeira versão deste documento respondeu à pergunta errada — pesquisou o serviço de **consulta de status** (`NfeConsultaProtocolo`), que já está implementado, mas não é o que o fluxo real de produção precisa. O fluxo real (motorista chega sem XML em mãos, sistema busca a nota **completa** na hora) depende de um serviço diferente (`NFeDistribuicaoDFe`), com regras de acesso mais restritas. A conclusão mudou — ver seção 3.

## 1. O que já existe hoje (confirmado no código, mobile + backend)

O TruckFlow **não emite** nota fiscal — só lê. Rastreado o fluxo completo (`tf-mobile/src/components/cards/scanner.tsx`, `tf-mobile/src/hooks/useNotaFiscal.ts`, `TruckFlow.Application/NotaFiscalService.cs`), existem hoje **dois fluxos independentes**, e só um deles toca a SEFAZ:

1. **Câmera (código de barras Code128 do DANFe) ou código digitado** → extrai só a chave de acesso (44 dígitos) → `GET /NotaFiscal/buscar-por-chave/{chave}` → `NotaFiscalService.ObterPorChaveAsync`. É uma **busca no próprio banco do TruckFlow** (`ObterPorChaveAcrossTenantsAsync`) — não bate na SEFAZ. Só funciona se a nota já tiver sido cadastrada antes (via XML). Se não achar, a UI mostra "Nota não encontrada".
2. **Upload de XML** → `POST /NotaFiscal/parse` → `NotaFiscalService.ParseXmlAsync`. É daqui que vem peso, fornecedor, placa, itens — extraído direto do conteúdo do arquivo XML (autocontido, já assinado). Não bate na SEFAZ.
3. **`POST /NotaFiscal/validar-sefaz/{chave}`** (`ValidarNaSefazAsync` → `ConsultarProtocoloAsync` → webservice `NfeConsultaProtocolo`) — exige certificado A1, mas só retorna **status** (autorizada/cancelada/denegada), não os dados da carga. **Não é chamado de nenhuma UI hoje** (nem mobile, nem admin) — está pronto no backend, sem tela ligada.

Isso cobre bem o caso "motorista já tem o XML em mãos (ou a nota já foi cadastrada por alguém antes)". **Não cobre** o fluxo real de produção descrito abaixo.

## 2. O fluxo real de produção (o que ainda falta construir)

Na operação real, o motorista chega **sem XML em mãos** — só com o papel/DANFe físico. O sistema precisa, na hora:
1. Ler a chave de acesso (câmera ou digitada).
2. Buscar a **nota completa** (peso, fornecedor, itens) diretamente na SEFAZ — não pode depender de alguém ter subido o XML antes, porque pode ser a primeira vez que essa nota passa pelo TruckFlow.
3. Usar esses dados pra liberar horário/doca/unidade conforme o que o ADM da fábrica já configurou pra aquele fornecedor/carga.

**Isso exige um serviço SEFAZ diferente do que estava implementado**: `NFeDistribuicaoDFe`, consulta por chave (`consChNFe`) — que retorna o **XML completo**, não só o status.

✅ **Implementado (2026-09-19):** `ISefazClient.ConsultarDistribuicaoAsync` (`ZeusSefazClient.cs`, usando `NfeDistDFeInteresse` da lib `Zeus.Net.NFe.NFCe` já referenciada no projeto), `NotaFiscalService.ParseFromSefazAsync`, endpoint `GET /v1/NotaFiscal/buscar-completa-sefaz/{chaveAcesso}`, wiring no mobile (fallback automático quando a nota não está no banco), e `FakeSefazClient.ConsultarDistribuicaoAsync` pra testar sem certificado. 275 testes automatizados + smoke test HTTP completo (parse → save → buscar-por-chave → validar-sefaz → idempotência) rodando com o motorista de teste. Detalhe em `Docs/nfe-distribuicao-dfe-backlog.md`.
Falta só a validação com um certificado A1 real — ver seções 4 e 5.

## 3. De quem precisa ser o certificado — resposta corrigida, por serviço

| Serviço | O que retorna | Implementado? | Quem pode consultar (fonte: Nota Técnica 2014.002 / tributos.io) |
|---|---|---|---|
| `NfeConsultaProtocolo` | Só status | ✅ Sim | Qualquer CNPJ com certificado válido — não precisa ser emitente/destinatário (MOC) |
| `NFeDistribuicaoDFe` / `consChNFe` | Nota completa (o que o fluxo real precisa) | ❌ Não | **Destinatário**: acesso pleno. **Emitente**: sem acesso por essa consulta. **Transportador ou terceiro autorizado**: acesso pleno, mas só se estiver identificado no próprio XML da nota (campo `transp` ou `autXML`) |

Pro fluxo real (item 2 acima), a TruckFlow **não é destinatário** (é a Aurora) nem está identificada como transportador/terceiro autorizado em cada nota dos fornecedores da Aurora — pedir isso a cada fornecedor não é operacionalmente viável. **Conclusão corrigida: pra esse serviço, o certificado muito provavelmente precisa ser da Aurora**, não da TruckFlow. O "não precisa ser da Aurora" da versão anterior valia só pro `NfeConsultaProtocolo` (status), que resolve uma parte menor do problema.

## 4. O que falta pra confirmar de verdade

Documentação é documentação — o teste real com a SEFAZ fecha a dúvida. **Não pode ser feito sem um certificado ICP-Brasil real** — não existe certificado "fake" aceito pela SEFAZ, nem no ambiente de homologação. Confirmado (2026-09-19): a SEFAZ valida a assinatura com um certificado real mesmo em homologação, e tem que ser **e-CNPJ** (a NF-e não aceita e-CPF, diferente de outras consultas mais simples). Ver seção 5 pra um caminho que não depende de esperar a Aurora.

Quando houver um certificado real disponível (da Aurora, ou um próprio — seção 5):

1. `dotnet user-secrets set "Sefaz:Certificado:Caminho" "<caminho do .pfx>"` + `"Sefaz:Certificado:Senha"` (nunca commitar).
2. `dotnet user-secrets set "Sefaz:CnpjConsultante" "<CNPJ do titular do certificado>"` (só dígitos — exigido pelo `NfeDistDFeInteresse`, não é inferido do certificado pela lib).
3. `dotnet user-secrets set "Sefaz:UseFake" "false"`, `Sefaz:Ambiente=2` (homologação primeiro).
4. Testar via `GET /v1/NotaFiscal/buscar-completa-sefaz/{chaveAcesso}` com uma chave real — confirmar que retorna o XML completo, não só status.
5. Se der erro de autorização/documento não encontrado inesperado: pode ser falta de manifestação do destinatário (evento formal de "ciência da operação" que algumas consultas exigem antes de liberar o XML completo) — vale perguntar ao time fiscal de quem for o certificado usado.

## 5. Como testar sem esperar a Aurora (certificado próprio)

Não existe certificado fake, mas existe um caminho barato e legítimo pra validar o código de verdade contra a SEFAZ real, sem depender do certificado da Aurora:

1. Comprar um **e-CNPJ A1 próprio da TruckFlow** (~R$150-300/ano) — mesmo que já foi cogitado pra `NfeConsultaProtocolo`. Precisa de CNPJ ativo (a TruckFlow como empresa) pra emitir.
2. Pedir credenciamento pro ambiente de **homologação** junto à SEFAZ do estado — liberação costuma ser automática, mas só sincroniza no dia seguinte.
3. Emitir pelo menos 10 notas de teste em homologação, **TruckFlow como emitente e destinatário ao mesmo tempo** (autoteste — prática comum de desenvolvedor, notas de homologação não têm valor fiscal). Várias ferramentas de emissão têm modo de teste gratuito pra isso.
4. Usar essas chaves de acesso reais pra testar `ConsultarDistribuicaoAsync`/`buscar-completa-sefaz` contra a SEFAZ de verdade.

**O que isso prova:** que o código funciona de ponta a ponta contra o webservice real (handshake TLS, parsing, descompressão gzip do `docZip`, mapeamento pro `NotaFiscalXmlExtractor`) — fecha praticamente todo o risco técnico do `ExtrairXmlDoLote` (`ZeusSefazClient.cs`), que hoje só foi validado por inspeção de schema, não contra a SEFAZ real.

**O que isso não prova:** que funciona especificamente com as notas *da Aurora* — nesse autoteste a TruckFlow é destinatária das próprias notas, não a Aurora. Isso só fecha com o certificado real deles. Mas depois desse autoteste, essa etapa final vira troca de config (`Certificado:*`, `CnpjConsultante`), não mais código — o risco que sobra é bem menor.

Não é algo que dá pra fazer por conta própria — certificado digital exige verificação de identidade real e CNPJ ativo, então esse passo depende de alguém da TruckFlow executar.

## Próximo passo

Pedir o certificado da Aurora **continua necessário** pra esse fluxo funcionar com dados reais deles — mantém o pedido já feito. Em paralelo, considerar o autoteste da seção 5 pra validar o código antes disso chegar. Vale também perguntar ao time fiscal da Aurora se eles já têm manifestação do destinatário configurada.
