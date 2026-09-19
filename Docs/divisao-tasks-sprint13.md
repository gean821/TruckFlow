# Divisão de tasks — Sprint 13 (backlog SAD Aurora)

> Critério: Gean (sênior, arquiteto do projeto) pega as tasks mais importantes/difíceis, tudo que toca `Program.cs` do backend, e **toda a parte de infra** (deploys, builds, homologação, produção, workflows de CI/CD). Felipe (jr/estagiário, com apoio de IA) pega tasks bem especificadas, isoladas em arquivos que o Gean não vai tocar, e de menor risco arquitetural.
>
> Ordem de execução sugerida dentro de cada bloco: de cima pra baixo (já respeita as dependências do backlog, ex.: `TEST-01` antes de `TEST-02`, `INFRA-03` antes de `INFRA-04`).

Status no Jira: assignees e prioridades já aplicados (TRUCK-208 a TRUCK-250). Priority mapeada como P0→Highest, P1→High, P2→Medium, P3→Low.

---

## Gean (sênior) — 25 tasks na Sprint 13

Foco: `Program.cs` do backend, segurança, concorrência/infra crítica, dados multi-tenant, **e toda a infra (deploy/build/hml/prod/workflows)**.

| Task | Épico | Prioridade | Por que é do Gean |
|---|---|---|---|
| SEC-01 | SEC | P0 | Reescrita de histórico do git + force-push coordenado — decisão de alto impacto |
| SEC-02 | SEC | P0 | Segurança ativa, resolve em minutos |
| SEC-03 | SEC | P0 | Mexe em `Program.cs`/middleware core |
| IAM-01 | IAM | P1 | Mexe em `Program.cs` (rate limiter) |
| IAM-02 | IAM | P1 | MFA/TOTP — lógica de auth sensível |
| IAM-05 | IAM | P2 | Mexe em `Program.cs` (Identity options) |
| CI-01 | CI | P1 | **Infra/workflow** — pipeline de build+test nos 3 repos |
| CI-02 | CI | P2 | **Infra/workflow** — extensão do CI-01 |
| TEST-01 | TEST | P1 | Setup de fixture com Testcontainers — infra de teste |
| TEST-02 | TEST | P1 | Casos de concorrência/cross-tenant — exige entender o domínio a fundo |
| INFRA-01 | INFRA | P0 | Backup — maior gap da planilha, decisão de infra |
| INFRA-02 | INFRA | P1 | Mexe em `Program.cs` (health checks) |
| INFRA-03 | INFRA | P1 | Mexe em `Program.cs` (migrations) — pré-requisito de outras tasks |
| INFRA-04 | INFRA | P1 | Multi-instância + proxy — deploy/infra |
| INFRA-05 | INFRA | P1 | Distributed lock — concorrência |
| INFRA-06 | INFRA | P2 | **Infra** — ambiente de homologação (hml) |
| OBS-01 | OBS | P2 | Mexe em `Program.cs` (OpenTelemetry) |
| OBS-02 | OBS | P2 | **Infra** — deploy de serviço de log centralizado (Seq/Loki) |
| OBS-03 | OBS | P2 | Mascaramento de dados sensíveis — decisão de segurança |
| CRYPTO-01 | CRYPTO | P2 | Vault de chaves — mexe em `Program.cs`/infra |
| DATA-01 | DATA | P2 | Exportação cross-entidade — risco de vazamento cross-tenant se mal feito |
| DATA-02 | DATA | P3 | Exclusão em cascata — mesmo risco do DATA-01 |
| DATA-03 | DATA | P3 | Depende do DATA-02 |
| NET-01 | NET | P2 | **Infra** — console do provedor de nuvem |
| NET-02 | NET | P2 | **Infra** — configuração de DNS |

## Felipe (jr/estagiário) — 10 tasks na Sprint 13

Foco: mobile, admin — sem tocar `Program.cs` nem infra/deploy. Todas têm spec clara no backlog (onde mexer, como implementar, critério de aceite) — dá pra usar IA como apoio, mas ele precisa entender o que fez pra explicar depois.

| Task | Épico | Prioridade | Por que é do Felipe |
|---|---|---|---|
| IAM-03 | IAM | P2 | `AuthStore.ts` do admin/mobile — arquivos que o Gean não toca |
| IAM-04 | IAM | P2 | Endpoint + tela novos, isolados |
| MOB-01 | MOB | P1 | Feature isolada no mobile, ADR já especifica o design |
| MOB-02 | MOB | P1 | Tela nova isolada, texto já especificado no ADR |
| MOB-03 | MOB | P3 | Baixo risco, cobertura parcial já é aceitável |
| MOB-04 | MOB | P3 | Mudança pequena e isolada |
| WEB-02 | WEB | P3 | Lint + primeira leva de aria-labels |
| WEB-03 | WEB | P2 | Endpoint novo isolado + troca de mock no front |
| WEB-04 | WEB | P3 | Só `vercel.json` |
| WEB-05 | WEB | P3 | Bugs pequenos e isolados |

---

## Backlog extra P3 (fora da Sprint 13 — próxima sprint ou quando sobrar tempo)

Identificado numa segunda varredura completa das 113 perguntas do SAD depois de fechar a Sprint 13. Não bloqueiam a saída da faixa "Crítica" — são melhoria adicional. Todos com Priority "Low" no Jira.

| Task | Épico | Assignee | Por quê |
|---|---|---|---|
| IAM-06 (SSO) | IAM | Gean | Maior esforço do lote (3-4 dias), mexe no core de autenticação |
| IAM-07 (controles granulares de acesso) | IAM | Gean | Novo middleware de autorização, decisão de segurança |
| CRYPTO-02 (criptografia em coluna) | CRYPTO | Gean | Mexe em `AppDbContext.cs`, impacto em queries existentes |
| OBS-04 (alertas de segurança) | OBS | Gean | Toca `AuditSaveChangesInterceptor` (mesmo arquivo que outras tasks do Gean) |
| INFRA-07 (controles de acesso entre ambientes) | INFRA | Gean | **Infra** — credenciais/acesso de staging, mesmo dono do INFRA-06 |
| WEB-06 (matriz de navegadores/SO) | WEB | Felipe | Documentação pura, 30min |
| VULN-01 (processo de triagem de vulnerabilidades) | VULN | Felipe | Documento/processo, depende do CI-02 (do Gean) já existir |

---

## Pontos de atenção pra não gerar conflito

- **`Program.cs` (backend):** só o Gean mexe (SEC-03, IAM-01, IAM-05, INFRA-02, INFRA-03, CRYPTO-01, OBS-01).
- **Infra/deploy/CI (workflows, hml, prod):** tudo com o Gean (CI-01, CI-02, INFRA-01 a 07, OBS-02, NET-01, NET-02).
- **DATA-01 (Gean) x WEB-03 (Felipe):** ambos tocam backend+admin, mas em controllers/endpoints diferentes (`/exportar-dados` vs `/relatorios/agendamentos`) — sem risco real, só avisar antes de abrir PR.
- **IAM-02 (Gean) x IAM-03 (Felipe):** IAM-02 pode tocar `LoginView.vue` do admin (campo de código 2FA); IAM-03 toca `AuthStore.ts`. Arquivos diferentes, ok.
