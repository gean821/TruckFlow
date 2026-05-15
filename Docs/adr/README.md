# ADRs — Backend TruckFlow

Architecture Decision Records do backend.

## Formato

Cada ADR segue o template **Michael Nygard** traduzido:

- **Status**: Proposto / Aceito / Substituído por ADR-XXXX / Obsoleto
- **Data**: data da decisão
- **Contexto**: o problema, restrições, dados que pesaram na escolha
- **Decisão**: o que foi decidido (concreto, acionável, sem hedge)
- **Consequências**: o que fica mais fácil, o que fica mais difícil
- **Alternativas consideradas**: por que não as outras opções
- **Referências**: links pra outros ADRs, código, discussões

**Mudou de ideia?** Crie um **novo ADR** com status "Substitui ADR-XXXX". Não edite o histórico — o que ficou ruim ensina tanto quanto o que deu certo.

## Índice

| # | Título | Status | Data |
|---|---|---|---|
| [0001](./0001-alvo-aurora.md) | Alvo Aurora Alimentos e dimensionamento para 21 fábricas | Aceito | 2026-05-14 |
| [0002](./0002-design-notificacoes.md) | Sistema de notificações: push + SSE in-app; WhatsApp via deep-link | Aceito | 2026-05-14 |
| [0003](./0003-design-tracking-motorista.md) | Rastreamento de motorista: foreground-only + TimescaleDB | Aceito | 2026-05-14 |
| [0004](./0004-prerequisitos-rollout-aurora.md) | Pré-requisitos de infra para rollout Aurora | Aceito | 2026-05-14 |
