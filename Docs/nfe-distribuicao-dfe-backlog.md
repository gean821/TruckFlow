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
5. ⏳ Troca pro certificado real da Aurora quando disponível (config, não código — mesmo padrão já usado hoje). Único item ainda bloqueado.

**Testes (2026-09-19):** 275 testes passando (240 pré-existentes + 35 novos: extractor, fake, `ParseFromSefazAsync`, e cobertura nova de `SaveParsedNotaAsync`/`ObterPorChaveAsync`/`ValidarNaSefazAsync`, que não tinham teste de serviço antes). Smoke test manual via HTTP real (registro de empresa + JWT real + 3 cenários no endpoint novo) confirmado funcionando. `npx tsc --noEmit` do mobile limpo.

## Bloqueio externo

Certificado A1 da Aurora — já solicitado por e-mail ao Gabriel. Não bloqueia o desenvolvimento (item 5 é só troca de config), só bloqueia a validação final ponta a ponta com a SEFAZ real.

## Pendência paralela, não bloqueante

Confirmar com o time fiscal da Aurora se eles já têm manifestação do destinatário configurada — pode ser pré-requisito adicional pra liberar o XML completo via `NFeDistribuicaoDFe`. Só descoberto no teste real com certificado.
