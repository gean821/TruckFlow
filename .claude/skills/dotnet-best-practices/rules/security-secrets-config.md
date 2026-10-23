---
title: Segredo nunca em appsettings/compose versionado — user-secrets em dev, env em prod
impact: CRITICAL
impactDescription: chave JWT/senha do banco/certificado A1 fora do git; rotação possível
tags: security, secrets, configuration, jwt, sad-aurora
---

## Segredo nunca em appsettings/compose versionado — user-secrets em dev, env em prod

Qualquer valor que dá acesso (JWT signing key, connection string com senha, senha/arquivo do certificado A1, API keys de Resend/Google, credencial Entra ID) fica **fora** do repositório. Dev: `dotnet user-secrets`. Prod: variáveis de ambiente `Secao__Chave` provisionadas pelo provedor (Railway hoje; Key Vault na migração Azure Brasil). `appsettings.json` só carrega chaves com valor vazio/placeholder e `ValidateOnStart` garante que foram preenchidas (ver `di-options-validate-on-start`).

**Incorreto:**

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=...;Password=SenhaReal123" },
  "JwtOptions": { "SecurityKey": "minha-chave-de-512-bits-..." }
}
```

**Correto:**

```bash
cd src/TruckFlowApi/TruckFlow
dotnet user-secrets set "JwtOptions:SecurityKey" "<gerada com 64+ bytes aleatórios>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=truckflow;Username=...;Password=..."
# prod: JwtOptions__SecurityKey, ConnectionStrings__DefaultConnection, Sefaz__Certificado__Senha
```

Se um segredo já foi commitado, **remover do arquivo não basta**: está no histórico — rotacione (ADR-0004 item 5 registra a rotação pendente da JWT key). Logs nunca imprimem configuração (`IConfiguration` dump) nem connection string.

**No TruckFlow:** `appsettings` já tem `JwtOptions:SecurityKey` vazio. `docker-compose.yml` usa `${POSTGRES_...}` de `.env` (não versionar `.env`). No mobile, `truckflow-driver-firebase-adminsdk-*.json` (service account admin) não deve morar na pasta do app.

Referência: [Safe storage of app secrets in development](https://learn.microsoft.com/aspnet/core/security/app-secrets) · [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html) · [Azure Key Vault configuration provider](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)
