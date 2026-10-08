# Ticket — Leitura de NF-e completa via SEFAZ (NFeDistribuicaoDFe)

> Pronto pra virar 1 ticket único no Jira (consolidado — antes estava dividido em 5, mas é um único desenvolvedor fazendo ponta a ponta).
>
> Origem: fluxo real de produção — o motorista chega sem XML em mãos, o sistema precisa buscar a nota completa (peso, fornecedor, itens) direto na SEFAZ, sem depender de alguém já ter cadastrado a nota via upload antes. Contexto e análise de certificado: `Docs/sefaz-certificado-consulta-nfe.md`.
>
> **Metodologia: TDD.** Testes escritos antes da implementação de cada método novo. Esta é uma área core do produto — arquitetura tem que ficar limpa, sem duplicação entre o fluxo de upload manual (já existente) e o fluxo de busca direta na SEFAZ (novo).

## Escopo

1. ✅ Novo método em `ISefazClient` pra buscar a nota **completa** (não só status) via `NfeDistDFeInteresse` — usando a biblioteca `Zeus.Net.NFe.NFCe` já referenciada no projeto (confirmado via inspeção do assembly: método e classes já existem, não precisa de pacote novo). `ZeusSefazClient.ConsultarDistribuicaoAsync`.
2. ✅ Refatoração de `NotaFiscalService`: extraído `NotaFiscalXmlExtractor` (`TruckFlow.Application/NotaFiscais/`) — parsing + extração puros, sem banco, compartilhados entre `ParseXmlAsync` (upload) e `ParseFromSefazAsync` (SEFAZ) via `EnriquecerComMatchingAsync`.
3. ✅ `FakeSefazClient.ConsultarDistribuicaoAsync` com fixture determinística (mesma convenção de sufixo de chave do método de status).
4. ✅ Endpoint `GET /v1/NotaFiscal/buscar-completa-sefaz/{chaveAcesso}` + wiring no mobile: `useNotaFiscal.ts` agora tenta `buscarNotaPorChave` (banco) primeiro e cai automaticamente pra `buscarNotaCompletaSefaz` em caso de 404 — motorista não precisa escolher "outra forma" manualmente quando a nota é nova.
5. ✅ **Validado contra a SEFAZ de produção real (2026-09-20)** — certificado A1 real (CR DA SILVA TRANSPORTES) + chave de acesso real. `validar-sefaz` retornou `cStat=100` com protocolo batendo exatamente com o DANFE real. `buscar-completa-sefaz` retornou `cStat=137` ("não localizado") — comunicação técnica funcionou (handshake/SOAP/parsing corretos), mas o documento não foi disponibilizado por essa consulta. Hipótese em teste: manifestação do destinatário pendente (não confirmada ainda) — ver `Docs/sefaz-certificado-consulta-nfe.md` seção 6 pro teste controlado planejado.

**6 bugs reais corrigidos em `ZeusSefazClient.cs`** durante essa validação (nenhum pego por teste automatizado — só apareceram batendo na SEFAZ real): `TipoCertificado`, `ModeloDocumento`/`tpEmis`, `ValidarSchemas`, `ProtocoloDeSeguranca`, `TimeOut` (estava em segundos, precisa ser milissegundos — causa do "timeout" persistente por várias rodadas), e o mais sutil, `ConfiguracaoCertificado.KeyStorageFlags` não setado (causa raiz do "SSL connection could not be established" — só achado decompilando a lib com `ICSharpCode.Decompiler`, já que ela não preserva a exceção real). Detalhe completo em `Docs/sefaz-certificado-consulta-nfe.md` seção 4.

**Testes (2026-09-20):** 275 testes automatizados passando (240 pré-existentes + 35 novos). Smoke test HTTP real completo (registro de empresa/motorista, JWT real, parse→save→buscar→validar, idempotência) + validação final contra SEFAZ de produção real com certificado real. `npx tsc --noEmit` do mobile limpo. **Ticket fechado tecnicamente** — não há mais nenhum item de código pendente.

## Pendência que não depende do TruckFlow

Quando testarmos com o certificado real da Aurora (destinatária de verdade das notas dos fornecedores dela), o esperado é que `buscar-completa-sefaz` funcione completo — mas se vier "não localizado" como no nosso teste, vale perguntar ao time fiscal deles sobre manifestação do destinatário. Não é código, é processo fiscal do lado do cliente.
