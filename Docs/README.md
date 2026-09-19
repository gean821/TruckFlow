# TruckFlow — Documentação (backend)

Este diretório contém ADRs (Architecture Decision Records) e documentos de design do backend .NET / PostgreSQL.

## Estrutura

- [`adr/`](./adr/README.md) — Decisões arquiteturais numeradas. Cada ADR captura **uma** decisão com contexto, alternativas e consequências.
- [`sad-aurora-backlog.md`](./sad-aurora-backlog.md) — Backlog técnico gerado a partir do questionário de segurança (SAD) da Aurora: itens jurídicos/comerciais separados dos itens de código, e os itens de código descritos como tasks prontas para Jira.
- [`entra-id-integracao-backlog.md`](./entra-id-integracao-backlog.md) — Impacto e backlog de tasks para a integração de login federado (SSO) com o Microsoft Entra ID da Aurora, pedida na reunião de alinhamento de TI. Substitui o item `IAM-02` do backlog do SAD.
- [`rastreamento-motorista-backlog.md`](./rastreamento-motorista-backlog.md) — Revisão da decisão de infra do [ADR-0003](./adr/0003-design-tracking-motorista.md): remove a dependência de TimescaleDB/PostGIS (feature é plus, não contratual) para não restringir a escolha de provedor com hospedagem obrigatória no Brasil.
- [`entra-id-fluxo-diagrama.mmd`](./entra-id-fluxo-diagrama.mmd) — Diagrama Mermaid do fluxo completo de login (local + federado Entra ID) para estudo e apresentação à TI da Aurora. Importar via "Mermaid to Excalidraw" (excalidraw.com/mermaid ou ícone na barra de ferramentas).
- [`migracao-azure-brasil.md`](./migracao-azure-brasil.md) — Passo a passo esboçado (executar só na assinatura do contrato) para migrar a hospedagem de Railway para Azure Brazil South, atendendo o requisito de residência de dados no Brasil da Aurora.

## Como ler

Comece pelo [índice de ADRs](./adr/README.md). O [ADR-0001](./adr/0001-alvo-aurora.md) define o cliente alvo (Aurora Alimentos) e o dimensionamento do produto — **leitura obrigatória para novos contribuidores**, porque dimensiona praticamente todas as outras decisões.

## Repos relacionados

A documentação é distribuída entre os três repos do produto:

| Repo | Localização | Escopo |
|---|---|---|
| **Backend** (este) | `TruckFlow/docs/adr/` | Domínio, persistência, notificação server-side, tracking server-side, infra |
| **Mobile** (motorista) | `tf-mobile/truckflow-driver-app/docs/adr/` | Captura de localização, push, deep-links, UX motorista |
| **Admin** (web) | `TruckFlowApp/truckflow.app/docs/adr/` | Consumo de SSE, mapa, contato WhatsApp, UX admin |

Cada repo tem seu próprio número de ADR independente. Quando uma decisão cruza repos, o ADR canônico vive no backend e os outros linkam.

