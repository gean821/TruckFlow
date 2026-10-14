---
name: webapp-testing
description: Toolkit para interagir e testar o TruckFlow rodando localmente (API .NET + painel admin Vue) com Playwright em Python. Use para verificar funcionalidade do front, depurar comportamento de UI, tirar screenshots, capturar logs do console e validar fluxos ponta a ponta (login, agendamento, recebimento, listagens paginadas).
license: Complete terms in LICENSE.txt
---

# Teste de aplicação web — TruckFlow

Para testar o TruckFlow localmente, escreva scripts Playwright nativos em Python.

**Script auxiliar**:
- `scripts/with_server.py` — gerencia o ciclo de vida de um ou mais servidores (sobe, espera a porta, roda o comando, derruba).

**Sempre rode os scripts com `--help` primeiro.** Não leia o código-fonte deles a não ser que precise de algo customizado — são caixas-pretas para não poluir o contexto.

## Topologia local

| Peça | Repo | Comando | Porta |
|---|---|---|---|
| Postgres 16 | `C:\ESTUDO\TruckFlow` | `docker compose up -d` | 5432 |
| API .NET 8 | `C:\ESTUDO\TruckFlow\src\TruckFlowApi\TruckFlow` | `dotnet run` | 8080 (Dockerfile/Program.cs) — confira `launchSettings.json` (perfil local usa 7100) |
| Admin Vue (Vite) | `C:\ESTUDO\TruckFlowApp` | `npm run dev` | 5173 |
| Mobile (Expo) | `C:\tf-mobile` | `npx expo start` | Metro 8081 — **não é testável por Playwright**; use Expo Web só para smoke ou teste no device |

O front lê `VITE_API_URL` (ex.: `http://localhost:8080/v1/`). Garanta que a porta da API bate com o `.env` do front e que a origem `http://localhost:5173` está no CORS (`TruckFlow/Extensions/Cors/AddCorsDependencyInjection.cs`).

## Árvore de decisão

```
Tarefa → A API e o front já estão rodando?
    ├─ Não → python scripts/with_server.py --help
    │        depois use o helper + script Playwright simplificado
    │
    └─ Sim → Reconhecimento-e-ação:
        1. Navegar e esperar networkidle
        2. Screenshot ou inspecionar o DOM
        3. Descobrir seletores no estado renderizado
        4. Executar ações com os seletores descobertos
```

## Exemplo: API + front juntos

`dotnet run` frio passa fácil de 30s — **sempre use `--timeout 120`**:

```bash
python scripts/with_server.py --timeout 120 \
  --server "cd C:/ESTUDO/TruckFlow/src/TruckFlowApi/TruckFlow && dotnet run" --port 8080 \
  --server "cd C:/ESTUDO/TruckFlowApp && npm run dev" --port 5173 \
  -- python fluxo_login.py
```

Script de automação (só lógica Playwright; os servidores já estão de pé):

```python
from playwright.sync_api import sync_playwright

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()
    logs = []
    page.on("console", lambda m: logs.append(f"[{m.type}] {m.text}"))
    page.on("response", lambda r: r.status >= 400 and logs.append(f"HTTP {r.status} {r.url}"))

    page.goto("http://localhost:5173/login")
    page.wait_for_load_state("networkidle")  # CRÍTICO: esperar o JS/Vuetify montar

    page.get_by_label("E-mail").fill("admin@empresa-alfa.test")
    page.get_by_label("Senha").fill("...")
    page.get_by_role("button", name="Entrar").click()
    page.wait_for_url("**/visualizar")

    page.screenshot(path="scratch/pos-login.png", full_page=True)
    print("\n".join(logs))
    browser.close()
```

## Particularidades do TruckFlow

- **Auth**: hoje o token está no `localStorage` (em migração para refresh token em cookie httpOnly). Para pular a tela de login em testes, prefira logar pela UI uma vez e salvar `context.storage_state(path=...)`; quando o cookie httpOnly entrar, o `storage_state` continua funcionando.
- **Multi-tenant**: todo fluxo crítico deve ser testado com **dois usuários de empresas diferentes** — o usuário da Empresa B não pode ver nada da A (listagens vazias, 404 em `GetById`). Isso é o requisito nº 1 da Aurora (ADR-0001).
- **Vuetify**: os inputs reais ficam dentro de `.v-field`; prefira `get_by_label`/`get_by_role` a CSS. `v-select`/`v-autocomplete` abrem um overlay `.v-overlay` anexado ao `body` — clique no campo, depois em `get_by_role("option", name=...)`.
- **Listagens paginadas com debounce (~350ms)**: depois de digitar no search, espere a resposta da API (`page.expect_response("**/v1/...")`) em vez de `wait_for_timeout`.
- **SSE (`/v1/notifications/stream`)**: a conexão fica aberta — `networkidle` pode nunca disparar em telas com stream. Nesse caso use `wait_for_selector` num elemento da tela.
- **Entra ID SSO**: o redirect para `login.microsoftonline.com` não é automatizável aqui; use o atalho de dev documentado em `Docs/entra-id-integracao-backlog.md` ou login local.

## Armadilha comum

❌ Inspecionar o DOM antes do `networkidle` em app dinâmico
✅ `page.wait_for_load_state("networkidle")` (ou `wait_for_selector` em telas com SSE) antes de inspecionar

## Boas práticas

- Use os scripts do `scripts/` como caixa-preta (`--help` primeiro).
- `sync_playwright()` para scripts síncronos; sempre feche o browser.
- Seletores descritivos: `get_by_role`, `get_by_label`, `get_by_text`; evite classes geradas.
- Salve screenshots/artefatos no scratchpad, não no repo.
- Teste de UI **não substitui** teste de integração do backend com Postgres real (`TruckFlow.Test`).
