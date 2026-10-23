# Migração de hospedagem: Railway → Azure Brazil South

> Origem: requisito da Aurora levantado na reunião de alinhamento de TI (2026-09-14) — dados e aplicação hospedados fisicamente no Brasil. Railway não tem região no Brasil (só Oregon, Ohio, Virginia, Frankfurt, Singapura), confirmado em `project_railway_deploy.md`. Rastreado como item aberto em `sad-aurora-backlog.md` seção 1.4 (itens 15/16 do SAD) e `rastreamento-motorista-backlog.md`.
>
> **Quando executar:** só na assinatura do contrato com a Aurora — enquanto isso o ambiente de teste continua no Railway, que é mais barato e já está funcionando. Este documento é o esboço pra não perder tempo decidindo isso de novo na hora.
>
> **Decisão:** Azure, região **Brazil South** (São Paulo). Motivo: já estamos integrados ao ecossistema Microsoft via Entra ID (`entra-id-integracao-backlog.md`), e o tier gratuito cobre os primeiros 12 meses pro tamanho atual (ver estimativa de custo no fim).

## Arquitetura alvo

```
Cloudflare (DNS/SSL/WAF — opcional, camada de borda)
        │
        ▼
Azure Brazil South (Resource Group: rg-truckflow-prod)
        │
        ├── Azure Container Apps (Environment: env-truckflow)
        │     └── TruckFlow API (imagem do Dockerfile já existente)
        │
        └── Azure Database for PostgreSQL Flexible Server
              (B1ms burstable, ≤32GB — free tier 12 meses)
```

Nada muda na aplicação .NET — o `Dockerfile` em `src/TruckFlowApi/TruckFlow/Dockerfile` já builda e expõe `/health/live` como healthcheck, então serve como está pro Container Apps. O trabalho é 100% infra/deploy.

## Passo a passo

### 1. Conta e Resource Group
1. Criar conta Azure nova (free tier: US$200/30 dias + serviços populares grátis por 12 meses).
2. Confirmar no momento da criação que **Brazil South** aparece disponível pros recursos abaixo (região do free tier pode variar — checar antes de prosseguir).
3. `az group create --name rg-truckflow-prod --location brazilsouth`

### 2. Registry de imagem
Duas opções — usar a mais barata:
- **GHCR (recomendado pra começar):** já temos GitHub Actions; publicar a imagem em `ghcr.io/<org>/truckflow-api` é grátis e não exige recurso Azure novo.
- **Azure Container Registry (ACR):** só se precisar de integração nativa mais estreita com Container Apps (pull sem token). Tier Basic é ~US$5/mês — não é o tier grátis.

Ficar com GHCR até o custo do ACR se justificar.

### 3. Banco — Postgres Flexible Server
```
az postgres flexible-server create \
  --resource-group rg-truckflow-prod \
  --name truckflow-db-prod \
  --location brazilsouth \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access <IP do Container Apps ou 0.0.0.0 temporário até configurar VNet integration>
```
- Confirmar que `Standard_B1ms` + `32GB` batem exatamente com o que o free tier cobre (750h/mês + 32GB) — qualquer SKU maior já sai do grátis.
- Depois de criado: rodar as migrations do EF Core apontando pra essa connection string (`dotnet ef database update` a partir de `TruckFlow.Infra`, igual já é feito local/Railway hoje) — banco sobe vazio, sem dado nenhum, então não tem passo de migração de dados nessa primeira vez.
- Se um dia precisar migrar dados reais do Railway pra cá: `pg_dump` do Railway + `pg_restore` no Azure (Postgres puro em ambos, sem extensão nenhuma envolvida — ver `rastreamento-motorista-backlog.md`, TimescaleDB/PostGIS já foi removido como dependência de propósito).

### 4. Container Apps
```
az containerapp env create \
  --name env-truckflow \
  --resource-group rg-truckflow-prod \
  --location brazilsouth

az containerapp create \
  --name truckflow-api \
  --resource-group rg-truckflow-prod \
  --environment env-truckflow \
  --image ghcr.io/<org>/truckflow-api:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 2
```
- `--min-replicas 0` aproveita a franquia "always free" (180k vCPU-s + 360k GiB-s + 2M requisições/mês) — a API escala a zero sem tráfego, sem custo parado.
- Se a latência de "cold start" depois de escalar a zero incomodar em produção real (não em teste), subir pra `--min-replicas 1` — aí sai do always-free, mas ainda é barato.

### 5. Variáveis de ambiente e secrets
Mesma convenção `__` que já é usada no Railway (ver `CLAUDE.md`, seção Config/segredos) — nada muda no código, só onde a variável é configurada:

| Variável | Origem | Segredo? |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | connection string do Postgres Flexible Server criado no passo 3 | ✅ `az containerapp secret set` |
| `JwtOptions__SecurityKey` | gerar nova chave 512-bit pra produção (não reaproveitar a do Railway) | ✅ secret |
| `EntraIdOptions__ClientId` / `EntraIdOptions__ClientSecret` | mesmo App Registration multi-tenant já criado (`entra-id-integracao-backlog.md`) — não recria nada, só aponta a credencial de produção | ✅ secret (ClientSecret) |
| `EntraIdOptions__FrontendLoginUrl` | domínio de produção do front (Vercel, já configurado) | não é segredo |
| `Cors__AllowedOrigins__0/1/2` | mesma lista de `appsettings.json` hoje — revisar se o domínio final muda | não é segredo |
| `Sefaz__Certificado__*` | certificado A1 real, quando sair de `UseFake: true` | ✅ secret |

Usar `az containerapp secret set` pros marcados como segredo, referenciados via `secretref` nas env vars — nunca em texto plano no `az containerapp create/update`.

### 6. DNS / domínio / TLS
- Container Apps já gera um domínio `*.azurecontainerapps.io` com HTTPS automático — suficiente pra validar antes de trocar domínio.
- Domínio próprio: `az containerapp hostname add` + certificado gerenciado (grátis, renovação automática).
- Cloudflare na frente é opcional — só adiciona WAF/DDoS/cache. Se usar, **não assumir que isso por si só satisfaz "dados no Brasil"**: por padrão a Cloudflare processa tráfego globalmente; existe "Regional Services" pra restringir processamento ao Brasil, mas é um recurso pago separado (Data Localization Suite). Se a exigência contratual da Aurora for literal, validar esse ponto especificamente antes de assumir que está coberto.

### 7. CI/CD — substituir o TODO do Railway
`Docs/../.github/workflows/cd.yml` hoje tem:
```yaml
run: |
  echo "TODO: deploy de producao hoje e do Railway (auto-deploy no push da master)."
  exit 1
```
Substituir pelo deploy real:
```yaml
- name: Login Azure
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

- name: Build e push imagem
  run: |
    docker build -f src/TruckFlowApi/TruckFlow/Dockerfile -t ghcr.io/<org>/truckflow-api:${{ github.sha }} .
    docker push ghcr.io/<org>/truckflow-api:${{ github.sha }}

- name: Deploy Container Apps
  run: |
    az containerapp update \
      --name truckflow-api \
      --resource-group rg-truckflow-prod \
      --image ghcr.io/<org>/truckflow-api:${{ github.sha }}
```
Usar **OIDC federado** (`azure/login@v2` com `client-id`/`tenant-id`/`subscription-id`, sem `client-secret`) em vez de Service Principal com secret de longa duração — evita ter mais um segredo de longa vida pra rotacionar.

### 8. Corte (cutover)
1. Deploy no Azure rodando em paralelo ao Railway, validar manualmente (login, CRUD básico, SSE, Entra ID) apontando o front pra URL do Azure.
2. Trocar `VITE_API_URL` do front (Vercel) pra apontar pro domínio novo.
3. Monitorar por alguns dias com os dois ambientes ainda de pé (Railway sem tráfego, só como rollback).
4. Desligar o serviço no Railway só depois de confirmar estabilidade — não deletar antes de ter certeza.

### Custo estimado (validado, não chutado)
- Primeiros 12 meses: **~R$0** dentro do free tier (Postgres B1ms/32GB coberto + Container Apps na franquia always-free), assumindo volume de teste/homologação.
- Depois dos 12 meses: Postgres B1ms sai ~US$12-25/mês; Container Apps continua majoritariamente dentro da franquia always-free pro volume atual (~1 fábrica). Escala junto com o rollout das 21 fábricas — reavaliar SKU nessa fase, não antes.

## Pendências que este documento não resolve
- Item 353 de `entra-id-integracao-backlog.md`: enquanto a migração não acontece, `EntraIdOptions:*` continua configurado no Railway.
- Regra de residência de dados "serviço por serviço" (backup, logs, monitoring) — checklist específico só faz sentido montar depois de confirmar com a Aurora se a exigência contratual é literal (todo componente) ou só API+banco. Não assumir uma leitura sem confirmar.
