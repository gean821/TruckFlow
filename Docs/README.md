# TruckFlow — Documentação (backend)

Este diretório contém ADRs (Architecture Decision Records) e documentos de design do backend .NET / PostgreSQL.

## Estrutura

- [`adr/`](./adr/README.md) — Decisões arquiteturais numeradas. Cada ADR captura **uma** decisão com contexto, alternativas e consequências.

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

