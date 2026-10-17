# Política de segurança — Autenticação local

Este documento descreve a política de senha e de bloqueio por tentativas aplicada ao login
local do TruckFlow (admin e motorista). É a evidência do item 24 do SAD.

## Política de senha

- Mínimo de **10 caracteres**.
- Exige pelo menos **1 caractere não alfanumérico**.
- **Email único** por conta (`RequireUniqueEmail = true`) — não é possível cadastrar duas
  contas com o mesmo email.

## Política de bloqueio por tentativas (lockout)

- **5 tentativas de senha incorreta** seguidas bloqueiam a conta.
- Conta bloqueada por **15 minutos**; novas tentativas durante o bloqueio são rejeitadas com
  a mensagem informando o tempo restante, sem checar a senha novamente.
- O contador de tentativas é **resetado a cada login bem-sucedido**.
- Aplicado em `LoginAdminAsync` e `LoginMotoristaAsync`
  (`TruckFlow.Application/UsuarioService.cs`), usando os mecanismos nativos do ASP.NET Core
  Identity (`UserManager.IsLockedOutAsync`, `AccessFailedAsync`, `ResetAccessFailedCountAsync`).

## Onde está configurado

`Program.cs` → `AddIdentity<Usuario, IdentityRole<Guid>>(options => ...)`:

```csharp
options.User.RequireUniqueEmail = true;
options.Password.RequiredLength = 10;
options.Password.RequireNonAlphanumeric = true;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

Erros de validação de senha no cadastro (`RegisterAdminAsync`/`RegisterMotoristaAsync`) são
traduzidos para `BusinessException` (HTTP 400 com a mensagem real do Identity), nunca para
erro 500.

## Rate limiting dos endpoints de login

O lockout acima só existe para contas que já existem (o estado fica no registro do `Usuario`).
Ele não protege contra um cliente martelando requisições em loop, nem contra tentativas em
usuário inexistente. Por isso os dois endpoints de login (`POST /v1/AuthAdmin/login` e
`POST /v1/AuthMotorista/login`) também têm rate limiting nativo do ASP.NET Core
(`Microsoft.AspNetCore.RateLimiting`), em duas camadas independentes (Sliding Window):

- **Por IP**: 5 requisições/minuto. Pega loop/script e tentativa em usuário inexistente.
- **Por conta**: 5 requisições/5 minutos, pela chave sozinha (não IP+conta) — pega ataque
  direcionado a uma conta vindo de IPs variados, sem travar todo mundo atrás do mesmo NAT
  corporativo só pela camada de IP.

Excedido o limite, a resposta é `429` com `Retry-After` e o envelope padrão
(`{"success": false, "code": "RATE_LIMITED", "message": "..."}`). Implementado em
`TruckFlow/Extensions/RateLimiting/AddRateLimitingDependencyInjection.cs` +
`TruckFlow/Middlewares/LoginRateLimitKeyMiddleware.cs`; testado em
`TruckFlow.Test/Integration/RateLimitingTests.cs`.

**Limitação conhecida**: o limiter é in-memory, por processo — não compartilhado entre
instâncias. Correto hoje (1 réplica). Quando o projeto for pra 2+ instâncias (ADR-0004), os
contadores não se somam entre elas (o limite efetivo multiplica pelo nº de instâncias); nesse
momento troca-se por um limiter com store distribuído (Redis).

## Fora do escopo: login via Microsoft Entra ID (SSO)

Esta política cobre **apenas o login local** (usuário/senha gerenciado pelo próprio TruckFlow).
Contas que entram via **SSO Microsoft Entra ID** não passam pelo `UserManager` do Identity nem
por este fluxo de senha/lockout — a autenticação, MFA e políticas de acesso condicional são
gerenciadas pelo próprio diretório (tenant) da Aurora Alimentos no Entra ID. Detalhes da
integração em `Docs/entra-id-integracao-backlog.md`.
