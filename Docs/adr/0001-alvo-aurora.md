# ADR-0001 — Alvo Aurora Alimentos e dimensionamento para 21 fábricas

- **Status**: Aceito
- **Data**: 2026-05-14

## Contexto

TruckFlow é construído com a **Aurora Alimentos** (2ª maior cooperativa do Brasil) como cliente alvo. O contrato prevê duas fases:

1. **Piloto**: 1 fábrica processando 80-100 caminhões/dia.
2. **Rollout pleno**: 21 fábricas operando simultaneamente, totalizando ~1.700-2.100 caminhões/dia.

A transição entre as fases é **gradual e lenta** (cronograma do cliente), mas o design não pode comprometer a fase 2 em troca de simplicidade na fase 1 — refatorar arquitetura no meio do rollout custa mais que dimensionar certo desde o piloto.

Características do cliente que pesam na arquitetura:

- **Cooperativa com marcas internas concorrentes**: as 21 fábricas pertencem a empresas/marcas distintas dentro da Aurora. Vazamento de dados entre `Empresa`s é vazamento competitivo real, não teórico.
- **Operação fiscal e operacional crítica**: NF-e, controle de descarga, planejamento. Dados sujeitos a auditoria fiscal e compliance LGPD.
- **Aurora exige RPO/RTO contratual**: sem isso, jurídico deles não assina.
- **Penetration test por terceiro é parte do due-diligence**: padrão para fornecedores TI da cooperativa.

## Decisão

Tratar o produto como sistema multi-tenant fiscal de produção desde o piloto, dimensionado para 21 fábricas:

**Modelo de tenancy**:
- Cada fábrica = 1 `Empresa` no domínio (modelo já existente).
- Filtro multi-tenant via `EmpresaId` em todas as entidades `IEmpresaScoped`.
- O bug atual do filtro retornando `true` quando `EmpresaId == Guid.Empty` deve ser corrigido para retornar zero linhas (ver ADR-0004).

**Dimensionamento alvo (regime, 21 fábricas)**:

| Métrica | Pico esperado |
|---|---|
| Caminhões/dia (total) | 1.700-2.100 |
| Motoristas ativos simultâneos | 300-400 |
| Reservas concorrentes | 50-80 |
| Admins online | 40-80 |
| Notificações/dia | 1.000-1.500 |
| Pings de localização (em rota) | 10-15 req/s |
| Inserts em `MotoristaPosicaoHistorico` | ~14 req/s sustentado |

Volume puro é trivial para qualquer single-instance — **o gargalo não é throughput**. O alvo arquitetural é confiabilidade, isolamento entre tenants, compliance LGPD e zero-downtime em deploy.

**Implicações que esse dimensionamento impõe** (detalhe em [ADR-0004](./0004-prerequisitos-rollout-aurora.md)):

1. Multi-instance + reverse-proxy é pré-requisito, não Onda 3.
2. Bug do filtro multi-tenant é blocker, não correção cosmética.
3. Migrations no startup viram race condition.
4. Backup/DR documentados são contratuais.
5. Observabilidade por fábrica permite SLO por tenant.

**Variação processual entre fábricas**: cada uma das 21 fábricas Aurora opera com processos parcialmente diferentes (escala de descarga, tipos de produto, regras de janela). A configuração específica fica em `Empresa.Configuracoes` (jsonb) — não em código condicional por fábrica.

## Consequências

**Positivas**:
- Decisões de design ficam mais simples de tomar (alvo claro).
- Bugs de multi-tenant que pareciam "cosméticos" no piloto viram blockers explícitos com SLA.
- Aurora vê desde a primeira sprint que o sistema está sendo construído para escala deles.
- Custo de mudança no meio do rollout é mitigado.

**Negativas**:
- Piloto consome ~16 dias extras de trabalho não-feature (lista em ADR-0004) que poderiam ser pulados se o alvo fosse "1 fábrica para sempre".
- Time precisa internalizar mentalidade multi-tenant desde o início — corte de atalho em controllers/services pode quebrar isolamento.
- Custo de hosting do piloto é maior que necessário para 80-100 caminhões/dia (Postgres managed com replica, observabilidade, etc.).

## Alternativas consideradas

**A1. Otimizar para o piloto, refatorar antes do rollout.**
Rejeitada. Aurora não vai esperar refatoração de 6-8 semanas entre piloto e rollout — eles vão querer rampar imediatamente após validação. Refatorar com 1 fábrica operando produção é mais arriscado que pagar o custo agora.

**A2. Modelar cada fábrica como instância separada do sistema (deploy por fábrica).**
Rejeitada. 21 instâncias = 21 deploys, 21 monitorings, 21 backups, 21 incidentes possíveis. Multi-tenant numa instância só é operacionalmente mais barato e o volume não justifica isolamento físico.

**A3. Modelar Aurora como uma única `Empresa` com sub-fábricas como entidade aninhada.**
Rejeitada. Marcas internas da cooperativa competem entre si — o isolamento de dados precisa ser do mesmo nível que entre empresas independentes. Modelar como uma `Empresa` só esconde requisito de compliance.

## Referências

- [ADR-0002 — Sistema de notificações](./0002-design-notificacoes.md)
- [ADR-0003 — Rastreamento de motorista](./0003-design-tracking-motorista.md)
- [ADR-0004 — Pré-requisitos de infra para rollout](./0004-prerequisitos-rollout-aurora.md)
