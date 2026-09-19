# Integração Microsoft Entra ID — Impacto no sistema e backlog de tasks

> Origem: pedido explícito da Aurora na reunião de alinhamento de TI (2026-09-14) — SSO via Entra ID, MFA delegado, provisionamento por grupo do AD, sem criação local de usuário. Substitui o item `IAM-02` (MFA caseiro via TOTP) do `Docs/sad-aurora-backlog.md`.
>
> **Escopo: afeta os 4 papéis "staff" da Aurora — `Admin`, `Gerente`, `Monitor`, `Portaria` (`Roles.cs`). Motorista continua 100% no login local — não tem conta Entra ID. Correção: versões anteriores deste documento citavam "Admin/Operador" genericamente — "Operador" não existe no sistema, os papéis reais são os 4 acima, confirmados em `Roles.cs`.**

## Status (2026-09-18) — implementado e testado de ponta a ponta com o handshake OIDC real

✅ **Handshake OIDC real fechado (2026-09-18, tarde):** tenant Entra ID de teste próprio (fora do Microsoft 365 Developer Program, que recusou a elegibilidade da conta — ver seção 8.2), App Registration multi-tenant, grupo `TruckFlow-Teste-Admin` e usuário `teste.sso.admin@geanlucaramosgmail.onmicrosoft.com` criados via Azure CLI. Login de verdade pelo front (`localhost:5173`, botão "Entrar com Microsoft") → redirect real pro `login.microsoftonline.com` → autenticação Microsoft real → callback com token real assinado → JIT provisionou o usuário (`Roles=Admin`, `EmpresaId` da Empresa Alfa) → JWT emitido → sessão restaurada → dashboard abriu. Fecha a única lacuna que faltava (linha 42, versão anterior deste documento) — o resto da lógica (multi-tenant, papéis, dois mundos de auth) já estava validado via `seed-entra-login`.

🐛 **Dois bugs reais encontrados e corrigidos só por causa desse teste de ponta a ponta** (nenhum dos dois aparece em teste unitário nem no `seed-entra-login`, que pula o OIDC de propósito):

1. **Extração errada do `oid`** (`EntraIdAuthInjection.cs`) — o `Microsoft.Identity.Web` remapeia o claim curto `oid` pra URI longa `http://schemas.microsoft.com/identity/claims/objectidentifier`. `FindFirstValue("oid")` não achava nada e caía no fallback `ClaimTypes.NameIdentifier`, que é o claim `sub` — estável por usuário+app, mas **não** o Object ID imutável do usuário no tenant (a Microsoft desaconselha explicitamente usar `sub` como chave de provisionamento multi-tenant). O primeiro login real provisionou o usuário com `AspNetUserLogins.ProviderKey = "p3PzS0YsZX7ciuwx5cLBmITgRMlhdnrDeI7ixW6VcMI"` (o `sub`) em vez do `oid` real (`16ff6473-5b2b-4bf0-90c7-9edbc5642623`). Corrigido lendo a URI longa primeiro, com fallback pro claim curto `oid` (compatibilidade com outros formatos de token).
2. **Redirect de sucesso desfeito pelo `finally`** (`LoginView.vue`, front) — o código fazia `router.replace('/visualizar')` dentro do `try` seguido de `return`, mas isso não pula o `finally`: o `finally` sempre roda, e tinha `router.replace({ path: '/login' })` incondicional, desfazendo o redirect de sucesso um instante depois. Sintoma: "faz todo o processo, mas volta pra tela de login" — apesar do backend emitir o JWT e o refresh funcionar 100%. Corrigido movendo o redirect pro `/login` só pros ramos de falha (`catch` e "não autenticado"), nunca no `finally`.

✅ `ENTRA-01` (vínculo via `AspNetUserLogins` + `Origem`/`UltimoSyncEntraEm` no `Usuario`), `ENTRA-01b` (tabela de campos), `ENTRA-02` (`EntraGroupRoleMapping` + repositório), `ENTRA-03` (pacote `Microsoft.Identity.Web`, `EntraIdAuthInjection.cs`, multi-scheme JWT+Cookie+OIDC — **registro condicional**: só ativa se `EntraIdOptions:ClientId` estiver preenchido, ver bug corrigido abaixo), `ENTRA-04`/`ENTRA-05` (`JitProvisioningService` completo: cria/sincroniza/desativa, usando `Usuario.DeletedAt` — o mesmo campo que o resto do app já usa pra ativo/inativo, não um campo novo), `ENTRA-06` (`Empresa.Configuracoes.authMethods` + `EmpresaAuthMethods` + gate nos dois pontos), `ENTRA-07` (botão "Entrar com Microsoft" + retorno em `LoginView.vue`, reaproveitando `restoreSession()`), `ENTRA-08` (`UserAdminResponseDto.IsEntraId` + `ManageUserView.vue` esconde editar/ativar-inativar nas linhas Entra ID, com chip "SSO"; guarda também no backend — `UpdateAdminAsync`/`DeleteAdminAsync`/`SetAdminStatusAsync` rejeitam via API direta, não só via UI escondida). Migration `entra-id-provisionamento` aplicada em Postgres local. Testes unitários de `EmpresaAuthMethods` (14 casos) + suite completa (240 testes) verdes. `vue-tsc --build` do front passa limpo.

🐛 **Bug real encontrado e corrigido durante o teste (2026-09-18):** registrar o esquema Entra ID com `ClientId` vazio quebrava a API **inteira** — todo endpoint, não só os do Entra ID — porque o `AuthenticationMiddleware` do ASP.NET Core precisa checar em toda requisição se algum esquema com `IAuthenticationRequestHandler` (Cookie/OIDC) deve interceptar a rota atual, e isso materializa as opções do OIDC, que o `Microsoft.Identity.Web` valida de forma eager e lança `IDW10106` sem `ClientId`. Corrigido registrando o esquema condicionalmente (só com `ClientId` preenchido). Sem esse teste com a API rodando de verdade, esse bug só apareceria em produção.

### O que foi testado de ponta a ponta, com dados reais (2026-09-18)

Duas empresas criadas via `/v1/registro` (login real, JWT real — não token forjado), usando `/v1/dev/seed-entra-login` pra simular o Entra ID:

| # | Cenário | Resultado |
|---|---|---|
| 1 | JIT nos 4 papéis (Admin, Gerente, Monitor, Portaria) | ✅ criados com role/EmpresaId corretos |
| 2 | Sincronização de papel ao mudar de grupo (mesmo usuário) | ✅ sem duplicar, role atualizada |
| 3 | Desativação ao perder o grupo | ✅ `DeletedAt` preenchido, roles zeradas, registro preservado |
| 4 | Tela somente-leitura via `GET /v1/usuarios` com JWT real | ✅ SSO com `isEntraId:true`, local com `false` |
| 5 | Bloqueio de escrita em usuário Entra ID via API direta (`PATCH`/`PATCH .../status`) | ✅ 400 `BUSINESS_ERROR` |
| 6 | CRUD normal em usuário local (criar/editar/inativar) | ✅ 200 em tudo |
| 7 | Isolamento multi-tenant (token da Empresa A buscando usuários) | ✅ zero usuários da Empresa B visíveis |
| 8 | Grupos ambíguos (grupo da Empresa A + grupo da Empresa B na mesma claim) | ✅ 400 `GruposAmbiguos` |
| 9 | Empresa sem `EntraId` habilitado tentando SSO | ✅ 400 `EmpresaNaoPermiteEntraId` |
| 10 | Empresa exigindo só SSO, login local tentado | ✅ 400 "Esta empresa exige login corporativo" |
| 11 | Empresa com os dois métodos habilitados | ✅ local e SSO funcionam ao mesmo tempo |

### Cobertura vs. o que a Aurora pediu na reunião

| Pedido da Aurora | Status | Observação |
|---|---|---|
| "Usuários não são criados locais, vê um grupo do AD e puxa um usuário do AD" | ✅ Testado (JIT simulado + handshake OIDC real) | Itens 1-3 da tabela acima + teste real de 2026-09-18 |
| "EntraId → provisionamento em grupos" | ✅ Testado | Itens 1-3, 8-11 + teste real com o grupo `TruckFlow-Teste-Admin` |
| "MFA vinculado à Aurora → Adaptável" | ⚠️ **Handshake OIDC real testado; MFA/Conditional Access específico ainda não** | Ver abaixo |
| "AD grupos → EntraId" | N/A — responsabilidade da Aurora | É o Entra ID Connect deles sincronizando o AD on-premises; não é algo que o TruckFlow controla, testa ou pode testar |
| "Deixar todos os dados no Brasil" / "Banco de dados do Brasil" | N/A — workstream separado | Ver `Docs/rastreamento-motorista-backlog.md` e `Docs/sad-aurora-backlog.md`, seção 1.4 — é decisão de hospedagem, não tem relação com o Entra ID |
| "Explicar toda a solução" | N/A — item de apresentação, não de código | Coberto pelo `Docs/entra-id-fluxo-diagrama.mmd` e pelos e-mails já trocados com o Gabriel |

**O que já foi fechado (2026-09-18):** o handshake OIDC real completo — redirect pro `login.microsoftonline.com`, autenticação Microsoft de verdade, token real assinado voltando com `oid`/`email`/`groups`, `Microsoft.Identity.Web` validando esse token, JIT provisionando a partir dele, JWT do TruckFlow emitido, sessão restaurada no front, dashboard abrindo. Isso cobre a validação de issuer multi-tenant e a extração de claims de um token real (não simulado), que era exatamente a lacuna registrada aqui antes.

**O que ainda falta, honestamente:** o tenant de teste usado (Entra ID gratuito, criado direto no portal — o Microsoft 365 Developer Program recusou a elegibilidade da conta usada) não tem licença para configurar Conditional Access/MFA adaptável — isso é um recurso do Entra ID P1/P2, que o tenant de teste gratuito não tem. Então o handshake OIDC em si está 100% validado com dados reais, mas a política de MFA/Conditional Access **específica da Aurora** (que roda do lado dela, no tenant dela) nunca foi exercitada, porque não há como simular isso sem acesso ao tenant real da Aurora. Isso não é um risco de bug no TruckFlow — o MFA é inteiramente delegado ao IdP, o código aqui não sabe nem precisa saber se MFA rodou ou não — é só um "não testamos com credencial real da Aurora" que só se resolve quando a Aurora liberar acesso ao tenant dela.

---

## Princípio arquitetural — não é opcional, é a decisão que sustenta tudo abaixo

**O TruckFlow continua sendo o dono da autenticação/autorização da aplicação. O Entra ID é uma fonte federada de identidade adicional, não um substituto.** Motivo de negócio, não só técnico: o TruckFlow é um SaaS multi-cliente — a Aurora é a cliente-alvo de hoje, mas transformar Entra ID na única forma de login acopla o produto à infraestrutura de um cliente específico. Um cliente futuro sem Entra ID corporativo (a maioria das empresas menores não tem) exigiria reimplementar autenticação do zero.

```
                    TRUCKFLOW
                       │
             ┌─────────┴─────────┐
             │                   │
       Login Local          Login Federado
       (usuário/senha)         (Entra ID)
             │                   │
             └─────────┬─────────┘
                       ↓
                Identidade validada
                       ↓
              TruckFlow AuthService
                       ↓
                JWT próprio (EmpresaId, Role, claims)
                       ↓
                    API
```

A API nunca sabe como o usuário se autenticou — ela só vê `Authorization: Bearer <jwt-do-truckflow>` e os claims de sempre (`UserId`, `EmpresaId`, `Role`). Autenticação ("como esse usuário provou quem é") e autorização ("o que ele pode fazer") ficam desacopladas por design: Entra ID resolve a primeira pra quem precisa de SSO corporativo, o `[Authorize(Roles=...)]` de sempre resolve a segunda, sem saber nem se importar de onde veio a identidade.

**Consequência prática, não só filosófica:** qual(is) método(s) de login uma empresa aceita é **configuração por `Empresa`, não uma decisão global do sistema** — ver `ENTRA-06` abaixo, que deixou de ser uma decisão em aberto e passou a ser parte obrigatória do desenho. Isso é o que transforma Entra ID numa *capacidade* do SaaS em vez de numa dependência dele.

O enum `OrigemUsuario` (`Local`, `EntraId`) já está desenhado pra crescer (`Google`, `Keycloak`, `Okta`, etc. no futuro) sem afetar `[Authorize]` nem o resto do modelo de autorização — mas **não construir esses provedores adicionais agora sem necessidade real**, é YAGNI puro até aparecer o segundo cliente que peça um IdP diferente.

---

## 0. Resumo do que muda e do que NÃO muda

**Não muda:**
- `[Authorize(Roles = "...")]` em todos os controllers — continua igual.
- Emissão de JWT + refresh token pelo TruckFlow — continua igual, o Entra ID só troca a validação de credencial na entrada, o token que circula na API depois continua sendo o seu.
- Fluxo de login do Motorista (`AuthMotoristaController`) — zero alteração.
- Modelo multi-tenant (`EmpresaId` como claim, query filters) — continua igual, só ganha uma fonte adicional de onde o `EmpresaId` de um usuário staff é decidido.

**Muda:**
- Como um usuário staff (`Admin`/`Gerente`/`Monitor`/`Portaria`) é criado (deixa de ser CRUD manual, vira JIT — provisionamento automático no primeiro login).
- Como esse usuário é autenticado (login federado com o Entra ID da Aurora, não usuário/senha local).
- Como MFA é aplicado (delegado à política de acesso condicional da Aurora, TruckFlow não implementa nada de MFA).
- Como esse usuário é desativado (reflexo automático do estado do grupo no Entra ID, não um botão "excluir").
- A tela de gestão de usuários no admin (para Admin/Operador vira majoritariamente somente leitura).

---

## 1. Modelo de dados

### `ENTRA-01` — Vincular `Usuario` ao login externo (usando o mecanismo nativo do Identity, não um campo customizado)
- **Prioridade:** P0 (bloqueia tudo abaixo) · **Repo:** backend
- **Onde:** `TruckFlow.Domain/Entities/Usuario.cs` + nova migration em `TruckFlowApi.Infra/Migrations/`
- **Correção de desenho:** a versão anterior deste documento propunha um campo customizado `EntraObjectId`. Revisando com mais cuidado: o `AspNetCore.Identity` (que o projeto já usa) tem uma tabela nativa pronta exatamente pra login externo — `AspNetUserLogins` — acessível via `UserManager.AddLoginAsync`/`FindByLoginAsync`. É menos código, é o padrão que qualquer dev .NET reconhece, e evita reinventar o que o framework já resolve.
  ```csharp
  // No momento do provisionamento:
  await _userManager.AddLoginAsync(usuario, new UserLoginInfo("EntraId", oid, "Entra ID"));

  // Na busca por um login já existente:
  var usuario = await _userManager.FindByLoginAsync("EntraId", oid);
  ```
  `oid` = claim imutável do Entra ID (nunca usar e-mail como chave — e-mail pode ser reciclado/alterado).

  Dois campos adicionais ainda valem a pena no `Usuario` (isso o Identity não cobre nativamente):
  ```csharp
  public OrigemUsuario Origem { get; set; }        // enum: Local, EntraId — usado só pra filtrar a UI (seção 3)
  public DateTime? UltimoSyncEntraEm { get; set; } // observabilidade: quando foi a última sincronização de papéis
  ```
- **Desativação:** usar o `LockoutEnd`/`LockoutEnabled` que o `AspNetCore.Identity` já traz pronto (setar `LockoutEnd = DateTimeOffset.MaxValue` desativa o login indefinidamente) — não precisa de um campo `Ativo` novo, e o `AuditSaveChangesInterceptor` já rastreia esses campos.
- **Critério de aceite:** migration aplicada sem quebrar usuários locais existentes; `FindByLoginAsync("EntraId", oid)` retorna `null` pra qualquer usuário local (nenhuma linha em `AspNetUserLogins`).
- **Esforço:** 0,5 dia.

### `ENTRA-01b` — Tabela de campos: de onde vem cada dado obrigatório do usuário federado

Levantamento feito em cima do único fluxo que hoje cria um `Usuario` + `Administrador` do zero (`SaasRegistrationService.cs`), pra não faltar nenhum campo obrigatório na hora de trocar por provisionamento via Entra ID:

| Campo | Entidade | Hoje vem de (registro manual) | Com Entra ID vem de | Obrigatório? |
|---|---|---|---|---|
| `Id` | `Usuario` | gerado | gerado (igual) | Sim |
| `UserName` | `Usuario` | digitado no formulário | claim `preferred_username` (UPN) | Sim |
| `Email` | `Usuario` | digitado | claim `email` | Sim |
| `PhoneNumber` | `Usuario` (via `IdentityUser`) | digitado, obrigatório **só por regra do DTO de registro** (não é `NOT NULL` no banco) | claim `phone_number`, **se existir no perfil Entra** | **Precisa virar opcional** pra usuário federado |
| `PasswordHash` | `Usuario` | `_userManager.CreateAsync(usuario, senha)` | nenhum — `CreateAsync(usuario)` sem senha é suportado nativamente pelo Identity | **Não se aplica** |
| `EmpresaId` | `Usuario` | empresa recém-criada no mesmo fluxo | `EntraGroupRoleMapping` (grupo → Empresa) | Sim |
| `CreatedAt` | `Usuario` | `DateTime.UtcNow` | igual | Sim |
| Role (Identity) | via `AddToRoleAsync` | escolhido manualmente (`Admin` + `Gerente` no registro inicial) | `EntraGroupRoleMapping` (grupo → Role, cobrindo `Admin`/`Gerente`/`Monitor`/`Portaria`) | Sim |
| `Administrador.Nome` | `Administrador` | digitado | claim `name` | Sim |
| `Administrador.UserName` | `Administrador` | duplicado do `Usuario.UserName` | mesma origem do `Usuario.UserName` | Sim |
| `Administrador.UsuarioId`/`Usuario` (nav) | `Administrador` | vínculo com o `Usuario` criado | igual | Sim |

Duas decisões que essa tabela obriga a tomar antes de implementar `ENTRA-04`:
1. Relaxar a obrigatoriedade de `PhoneNumber` na validação usada pelo fluxo federado (não é constraint de banco, é regra do DTO atual — o DTO do fluxo federado é outro, não precisa herdar essa regra).
2. Confirmar que `Administrador` é usado como perfil genérico pra qualquer um dos 4 papéis staff (é o que o código faz hoje — não existe entidade `Gerente`/`Monitor`/`Portaria` separada).

### `ENTRA-02` — Tabela de mapeamento grupo Entra → Empresa + Papel
- **Prioridade:** P0 · **Repo:** backend
- **Onde:** nova entidade `TruckFlow.Domain/Entities/EntraGroupRoleMapping.cs`
- **Como implementar:**
  ```csharp
  public class EntraGroupRoleMapping
  {
      public Guid Id { get; set; }
      public Guid EmpresaId { get; set; }      // para qual fábrica/tenant esse grupo dá acesso
      public string EntraGroupId { get; set; } // Object ID do grupo no Entra ID (GUID da Aurora)
      public string EntraGroupNome { get; set; } // nome legível, só pra exibir na UI ("TruckFlow-Admin-Mandaguari")
      public string RoleName { get; set; }     // "Admin", "Gerente", "Monitor" ou "Portaria" (Roles.cs)
      public bool Ativo { get; set; }
  }
  ```
  **Por que precisa do `EmpresaId` aqui e não só o papel:** o modelo de vocês trata cada fábrica Aurora como uma `Empresa` isolada (ADR-0001). Um grupo do Entra ID precisa resolver **fábrica + papel** ao mesmo tempo — "esse grupo é Admin, mas de qual das 21 fábricas?" Sem isso, um mapeamento errado vira vazamento cross-tenant, exatamente o risco que o isolamento multi-tenant já existente foi desenhado pra evitar.

  **Escala:** com 4 papéis por fábrica, são potencialmente **4 grupos × 21 fábricas = até 84 grupos** no Entra ID da Aurora em regime pleno. Vale alinhar com o Gabriel uma convenção de nome desde já (ex.: `TruckFlow-{Fabrica}-{Papel}`) pra não improvisar isso fábrica por fábrica.
- **Critério de aceite:** dado um `EntraGroupId`, a query retorna exatamente 1 `EmpresaId` + 1 `RoleName` (ou nenhum, se o grupo não está mapeado — nesse caso o usuário não ganha acesso a nada).
- **Esforço:** 0,5 dia (entidade + migration + repositório básico).

---

## 2. Autenticação e provisionamento

### `ENTRA-03` — Registrar autenticação OpenID Connect com o Entra ID (multi-scheme)
- **Prioridade:** P0 · **Repo:** backend
- **Onde:** novo `TruckFlow/Extensions/Auth/EntraIdAuthInjection.cs`, ao lado do `AuthInjection.cs` (JWT) que já existe
- **Ponto de arquitetura, não esquecer:** o esquema OIDC é usado **só no momento do login** (challenge + callback). Depois de autenticado, o TruckFlow continua emitindo e validando **seu próprio JWT Bearer** pra todas as chamadas de API, exatamente como hoje. Isso preserva 100% do modelo de `EmpresaId`/refresh token/revogação já construído — não trocamos JWT próprio por token do Entra ID circulando pela API.
- **Detalhe técnico que faltava explicitar:** `AddMicrosoftIdentityWebApp` registra, por baixo dos panos, **dois schemes novos** — `OpenIdConnect` (challenge, redireciona pro login da Aurora) e `Cookies` (guarda o estado da sessão só durante o intervalo entre o redirect e o callback, é descartado depois que o JWT próprio é emitido). Isso convive sem conflito com o `JwtBearer` que já é o scheme default da API — são três schemes registrados, cada um com um papel diferente e nenhum atropela o outro (ver leitura #4 abaixo, "múltiplos schemes").

#### Pacotes NuGet
```
dotnet add src/TruckFlowApi/TruckFlow package Microsoft.Identity.Web
```
**Não instalar `Microsoft.Identity.Web.MicrosoftGraph` ainda.** Comece só com o pacote core — login, callback, claim de grupos via "Groups assigned to the application" (ver alerta de robustez abaixo) resolve o caso de uso sem precisar chamar a Graph API. Só adicione o pacote de Graph se, na prática, aparecer um caso real de overage (alguém com muitos grupos) — YAGNI: não instale a dependência antes de comprovar que precisa dela.

#### Passo a passo — o que precisa existir antes de escrever qualquer linha de código
1. **Criar o App Registration no Entra ID, no tenant da própria TruckFlow** (não no da Aurora — isso é o que permite a Aurora só dar consentimento, sem criar nada do lado deles, conforme já alinhado por e-mail com o Gabriel):
   - Portal Azure/Entra → *App registrations* → *New registration*.
   - **Supported account types:** "Contas em qualquer diretório organizacional" (multi-tenant) — é isso que permite qualquer tenant (o da Aurora, e no futuro outros clientes) dar consentimento sem vocês precisarem recriar o app.
   - **Redirect URI:** tipo *Web*, apontando pro seu callback (ex.: `https://api.truckflow.app/signin-oidc` em produção + `https://localhost:{porta}/signin-oidc` em dev).
   - Anotar o **Application (client) ID** gerado.
2. **Gerar um Client Secret** (Certificates & secrets → New client secret) — guardar o valor na hora, ele não aparece de novo depois. Recomenda-se migrar pra certificado mais adiante, secret é aceitável pra começar.
3. **Configurar a claim de grupos com segurança** (Token configuration → Add groups claim):
   - Selecionar **"Groups assigned to the application"**, não "All groups". Isso limita a claim aos grupos que vocês mesmos atribuírem ao app (os ~84 grupos `TruckFlow-{Fabrica}-{Papel}`), evitando de vez o limite de 200 grupos por token do Entra ID (ver alerta abaixo).
   - Marcar pros três tipos de token (ID, Access) já que vamos usar OIDC.
4. **API permissions:** `openid`, `profile`, `email` (delegated, já vêm por padrão) +, se for usar o plano B da Graph, `GroupMember.Read.All` (delegated) com consentimento de admin.
5. Configuração nova no projeto (seguir o padrão já usado pra `JwtOptions`):
   ```json
   "EntraIdOptions": {
     "TenantId": "",
     "ClientId": "",
     "ClientSecret": "",
     "CallbackPath": "/signin-oidc"
   }
   ```
   Vazio no `appsettings.json`, populado via `dotnet user-secrets` em dev e variável de ambiente em prod — mesmo modelo do README atual.

#### Código (esqueleto)
```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("EntraIdOptions").Bind(options);
        options.ResponseType = "code";
    });
// JWT Bearer (scheme default, usado pela API) continua registrado pelo AuthInjection.cs já existente
```

#### ⚠️ Alerta de robustez — leia antes de implementar
O Entra ID **limita a 200 o número de grupos emitidos num JWT** (150 pra SAML), incluindo grupos aninhados (fonte: Microsoft Learn, ver leitura #4 abaixo). Alguém que atua em várias fábricas (ex.: gerente regional em 5+ fábricas, cada uma com múltiplos grupos) pode se aproximar desse limite. Se isso acontecer, o Entra ID **troca a claim de grupos por um indicador de overage silenciosamente** — o login não dá erro óbvio, só some a informação que o `JitProvisioningService` precisa. Mitigar assim, dos mais simples pros mais robustos:
1. Usar **"Groups assigned to the application"** no token (passo 3 acima) — resolve quase sempre, porque limita a claim só aos grupos do TruckFlow, não a todos os grupos da Aurora.
2. Se ainda assim for um risco (ex.: Aurora decidir usar um esquema de grupo mais granular no futuro), usar `Microsoft.Identity.Web.MicrosoftGraph` pra consultar `/me/memberOf` direto na Graph API, que não tem esse limite.
- **Critério de aceite:** login funciona e retorna os grupos corretos mesmo simulando um usuário de teste com dezenas de grupos atribuídos.
- **Esforço:** 1,5 dia (mais tempo de configuração no portal do que código).

#### Leitura recomendada, nesta ordem
1. [Microsoft.Identity.Web — wiki oficial](https://github.com/AzureAD/microsoft-identity-web/wiki) — começar pela seção "Web apps". É a referência principal da lib, mantida pela própria Microsoft.
2. Microsoft Learn — busque por "Quickstart: Add sign-in with Microsoft Entra ID to an ASP.NET Core web app" (o passo a passo oficial do fluxo que vamos implementar; o slug exato da URL muda ocasionalmente, mais fácil buscar direto no Microsoft Learn).
3. [Configurar claims de grupo (optional claims)](https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims) — **leitura obrigatória**, é a fonte confirmada do limite de 200 grupos por token e de como configurar "Groups assigned to the application". Ler a seção "Configure groups optional claims".
4. [ASP.NET Core — múltiplos schemes de autenticação](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/policyschemes) — como combinar o scheme OIDC (login) com o JWT Bearer (API) no mesmo app sem um atropelar o outro.
5. Microsoft Graph — busque por "Microsoft Graph API list a user's memberOf" (documentação do endpoint usado no plano B de robustez, se `Microsoft.Identity.Web.MicrosoftGraph` for necessário).
6. [Visão geral de Conditional Access](https://learn.microsoft.com/en-us/entra/identity/conditional-access/overview) — não é algo que você configura, é o que a Aurora configura do lado dela pro MFA adaptável. Vale entender pra conversar com o Gabriel com propriedade.

### `ENTRA-04` — Endpoint de callback + serviço de provisionamento JIT
- **Prioridade:** P0 (depende de `ENTRA-01`, `ENTRA-02`, `ENTRA-03`) · **Repo:** backend
- **Onde:** novo `AuthEntraController.cs` (`GET /v1/AuthAdmin/entra/login`, `GET /v1/AuthAdmin/entra/callback`) + novo `JitProvisioningService` em `TruckFlow.Application`
- **Como implementar** — passo a passo do que o callback faz:
  1. Recebe o retorno do Entra ID já validado pelo middleware OIDC (claims: `oid`, `email`, `name`, grupos).
  2. `JitProvisioningService.SincronizarAsync(oid, grupos)`:
     - Busca `Usuario` via `_userManager.FindByLoginAsync("EntraId", oid)` (ver `ENTRA-01`).
     - Não existe → cria novo `Usuario` sem senha (`Origem = EntraId`), com `PhoneNumber` opcional, e associa o login externo via `AddLoginAsync`.
     - Resolve `EmpresaId` + `Role` via `EntraGroupRoleMapping` para cada grupo presente no token.
     - Nenhum grupo mapeado → rejeita o login (usuário existe no Entra ID da Aurora, mas não tem grupo autorizado pro TruckFlow) — retornar 403 com mensagem clara.
     - Sincroniza roles do Identity (`UserManager.AddToRoleAsync`/`RemoveFromRoleAsync`) para bater exatamente com os grupos atuais — roda em **todo** login, não só na primeira vez.
     - Atualiza `UltimoSyncEntraEm`.
  3. Emite o JWT + refresh token do TruckFlow **reaproveitando o `IAuthService`/`IRefreshTokenService` já existentes** — o callback chama exatamente o mesmo método que o login local usa depois de validar a senha. Não duplicar lógica de emissão de token num caminho separado; o ponto de entrada muda (senha vs. Entra ID), o ponto de emissão do JWT é o mesmo de sempre.
- **Critério de aceite:** primeiro login de um usuário novo cria o `Usuario` automaticamente com `Empresa`/`Role` corretos; segundo login de alguém que mudou de grupo reflete o novo papel; login de alguém sem grupo mapeado é rejeitado com 403.
- **Esforço:** 2 dias.
- **Antes de codar isso: valide o `ENTRA-03` isolado.** Não implemente o JIT completo de uma vez. Ordem recomendada dentro do próprio callback:
  1. Primeiro, o callback só loga as claims recebidas (`oid`, `email`, `name`, `groups`) sem persistir nada — confirma que o Entra ID está devolvendo o que se espera, principalmente o formato do `groups` (vem como lista de GUIDs, não nomes).
  2. Só depois de confirmar isso manualmente (com um usuário de teste real da Aurora), escrever a lógica de resolução de grupo → Empresa/Role.
  3. Só então acoplar a criação/sincronização do `Usuario` (o JIT propriamente dito).
  Isso limita o raio de um erro a uma camada por vez, em vez de debugar OIDC + grupos + JIT + emissão de JWT tudo junto na primeira tentativa.

### `ENTRA-05` — Desativação automática por ausência de grupo (nunca hard-delete)
- **Prioridade:** P0 · **Repo:** backend
- **Onde:** dentro do `JitProvisioningService` (mesma lógica do `ENTRA-04`)
- **Como implementar:** se, na sincronização, o usuário **já existe localmente** mas nenhum grupo do token bate com `EntraGroupRoleMapping`, setar `LockoutEnd = DateTimeOffset.MaxValue` (via `UserManager.SetLockoutEndDateAsync`) e remover os papéis — **nunca apagar a linha do `Usuario`** (referenciado em agendamentos/auditoria/notificações; apagar quebra histórico fiscal). Mesmo princípio já usado na exclusão de dados de localização do motorista (LGPD): mantém rastro de auditoria, remove acesso.
  - Janela de exposição: como o access token já expira em 15 minutos (`JwtOptions:AccessLifetimeMinutes`), o corte de acesso acontece no máximo nesse intervalo, sem precisar de nada em tempo real.
  - **Se a Aurora exigir corte imediato** (não esperar os 15 min): tarefa opcional futura — job em background consultando Microsoft Graph periodicamente para desativar proativamente. Não construir isso agora; só se for pedido explicitamente.
- **Critério de aceite:** usuário removido do grupo autorizado perde acesso em até 15 minutos, sem excluir o registro histórico.
- **Esforço:** incluso no `ENTRA-04` (mesma classe), sem esforço adicional relevante.

### `ENTRA-06` — Métodos de login habilitados por `Empresa` (decisão tomada, não mais em aberto)
- **Prioridade:** P0 — isso não é um extra, é o que impede o Entra ID de virar uma dependência estrutural do produto. Sem isso, qualquer cliente futuro sem Entra ID corporativo fica travado. · **Repo:** backend
- **Onde:** `Empresa.Configuracoes` (jsonb, já existe conforme ADR-0001) + `AuthAdminController.Login` + o callback do `ENTRA-04`
- **Desenho:**
  ```json
  // Empresa.Configuracoes
  {
    "authMethods": ["Local", "EntraId"]
  }
  ```
  - Default para toda `Empresa` que nunca configurou nada: `["Local"]` — garante retrocompatibilidade total, nenhum cliente existente é afetado por essa mudança.
  - Aurora pode ser configurada como `["EntraId"]` (só federado) ou `["Local", "EntraId"]` (os dois, útil como plano B durante a transição/piloto).
  - Um cliente futuro sem Entra ID nunca vê essa opção, nunca precisa saber que ela existe — continua em `["Local"]` pra sempre, sem nenhum código condicional extra pra eles.
- **Como implementar — os dois pontos de enforcement:**
  1. `AuthAdminController.Login` (usuário/senha): antes de validar a senha, checar se `"Local"` está em `Empresa.Configuracoes.authMethods` do usuário. Se não estiver, rejeitar com mensagem clara ("Esta empresa exige login corporativo (SSO)"), não com erro genérico de credencial inválida.
  2. Callback do Entra ID (`ENTRA-04`): depois de resolver a `EmpresaId` via `EntraGroupRoleMapping`, checar se `"EntraId"` está habilitado pra aquela `Empresa` antes de emitir qualquer token — defesa em profundidade, pro caso de alguém criar uma linha de mapeamento sem atualizar a configuração da empresa correspondente.
- **Critério de aceite:** usuário de uma `Empresa` configurada só com `["EntraId"]` não consegue logar via `AuthAdminController.Login` mesmo com senha correta; usuário de uma `Empresa` só com `["Local"]` não consegue completar o fluxo do Entra ID mesmo pertencendo a um grupo mapeado. Cobrir os dois casos em `ENTRA-09`.
- **Esforço:** 0,5 dia.

---

## 3. Frontend admin

### `ENTRA-07` — Botão "Entrar com conta Aurora" na tela de login
- **Prioridade:** P0 · **Repo:** admin (`TruckFlowApp/truckflow.app`)
- **Onde:** `LoginView.vue`
- **Como implementar:** botão que redireciona pro `GET /v1/AuthAdmin/entra/login` do backend; tratar o retorno do callback (token + refresh cookie já vêm prontos do backend, igual ao fluxo de login local hoje — não precisa reinventar o armazenamento de token no front, é o mesmo `AuthStore` que já existe).
- **Esforço:** 1 dia.

### `ENTRA-08` — Tela de usuários vira majoritariamente leitura para os papéis staff
- **Prioridade:** P1 · **Repo:** admin
- **Onde:** tela de gestão de usuários existente
- **Como implementar:** remover/ocultar os botões de criar/excluir manualmente para usuários com `Origem = EntraId`; manter só visualização (Empresa, papel — `Admin`/`Gerente`/`Monitor`/`Portaria`, grupo de origem, `UltimoSyncEntraEm`, status ativo/inativo via `LockoutEnd`). Essa tela se torna, na prática, o relatório de recertificação de acesso que já estava planejado no `IAM-04` do `sad-aurora-backlog.md` — dá pra fundir as duas tasks.
  - Usuários `Origem = Local` (contas internas da TruckFlow, se mantidas) continuam com CRUD normal.
  - Motorista: nenhuma mudança.
- **Esforço:** 1 dia.

---

## 4. Testes

### `ENTRA-09` — Cobertura de testes de integração do fluxo JIT
- **Prioridade:** P1 · **Repo:** backend · depende do `TEST-01` (setup de `DatabaseFixture`) já previsto no `sad-aurora-backlog.md`
- **Casos mínimos:**
  1. Login novo com grupo mapeado → cria `Usuario` com `EmpresaId`/`Role` corretos.
  2. Login de usuário existente que mudou de grupo → papéis atualizados, sem duplicar usuário.
  3. Login sem grupo mapeado → 403, nenhum `Usuario` criado/ativado.
  4. Usuário removido do grupo → próximo login/refresh reflete `LockoutEnd` setado, sem apagar o registro.
  5. **Gate multi-tenant (crítico):** grupo mapeado pra `Empresa A` nunca pode resultar em acesso a dados de `Empresa B` — mesmo teste de isolamento que já existe pro resto do sistema, agora cobrindo também a origem Entra ID.
  6. **Gate dos dois mundos (`ENTRA-06`):** `Empresa` configurada só com `EntraId` rejeita login local mesmo com senha correta; `Empresa` configurada só com `Local` rejeita o callback do Entra ID mesmo com grupo mapeado válido; `Empresa` com os dois métodos habilitados aceita ambos sem conflito.
- **Esforço:** 2 dias (o item 6 é novo em relação à estimativa anterior).

---

## 5. Documentação

### `ENTRA-10` — ADR da decisão
- **Prioridade:** P2 · **Repo:** backend
- **Onde:** `Docs/adr/0005-autenticacao-entra-id.md` (segue a numeração existente: 0001-alvo-aurora, 0002-notificacoes, 0003-tracking, 0004-prerequisitos)
- **Conteúdo:** contexto (pedido da Aurora), decisão (multi-scheme OIDC + JWT próprio + JIT provisioning + mapeamento grupo→Empresa/Role), alternativas consideradas (ex.: aceitar token do Entra ID direto na API — rejeitada, porque quebraria o modelo de refresh token e `EmpresaId` já construído), consequências.
- **Nota à parte:** o `CLAUDE.md` atual ainda descreve "Expiração: 4h fixas (sem refresh token — em implementação)" na seção de Autenticação — isso já está desatualizado (refresh token já existe e funciona). Vale atualizar essa seção junto com o ADR novo, pra não desalinhar quem ler o `CLAUDE.md` depois.
- **Esforço:** 0,5 dia.

### `ENTRA-11` — Atualizar `sad-aurora-backlog.md`
- **Prioridade:** P2 · **Repo:** backend
- **O que fazer:** substituir o item `IAM-02` (MFA caseiro TOTP) por uma referência a este documento; atualizar os itens 22/23/19/20/21 do SAD para refletir o novo nível de resposta esperado depois de implementado (MFA/SSO deixa de ser "em branco" e passa a ser `C`/`D`, já que é delegado a um IdP corporativo real, não construído do zero).
- **Esforço:** 0,5 dia.

### `ENTRA-12` — Recriar o App Registration num tenant Entra ID oficial da empresa (não no tenant de teste pessoal)
- **Prioridade:** P1 (bloqueia enviar o link de consentimento admin pra Aurora) · **Repo:** backend + Azure
- **Contexto:** o App Registration multi-tenant usado em todos os testes até aqui (2026-09-18) foi criado no tenant de teste gratuito `geanlucaramosgmail.onmicrosoft.com` (Tenant ID `5d71232b-0b7d-4de9-ba86-682017f88b29`), só pra validar o handshake OIDC sem depender da Aurora. Isso é adequado pra teste, mas **não deve ser o que a Aurora vê na tela de consentimento admin** — o domínio "publicador" do app apareceria como esse tenant pessoal, o que não é profissional pra um cliente real aprovar.
- **O que fazer:** antes de mandar o link de admin-consent pra Aurora, recriar o mesmo App Registration (multi-tenant, "Contas em qualquer diretório organizacional", mesmas API permissions e claim de grupos "Groups assigned to the application") num tenant Entra ID que seja oficialmente da empresa TruckFlow (domínio próprio, não pessoal). Depois, atualizar `EntraIdOptions:ClientId`/`ClientSecret` em produção (via env var, nunca em `appsettings.json` — ver `CLAUDE.md`) pra apontar pro novo `ClientId`. Nenhuma outra mudança de código é necessária (o app continua multi-tenant, `TenantId: "organizations"`).
- **Esforço:** ~1h (é recriação de configuração, não desenvolvimento).

---

## 6. Plano de execução em PRs incrementais

Em vez de uma PR gigante, divida em 5 PRs pequenas e testáveis isoladamente — se algo der errado, fica óbvio em qual camada quebrou:

| PR | Conteúdo | Tasks | O que valida antes de seguir pra próxima |
|---|---|---|---|
| **PR 1** | Modelo de dados + registro OIDC + login/callback **sem persistir nada** | `ENTRA-01`, `ENTRA-03` | Login redireciona pro Entra ID, volta autenticado, e o callback consegue **logar** `oid`, `email`, `name` e `groups` (confirmar que `groups` vem como lista de GUIDs). Nenhum `Usuario` é criado ainda. |
| **PR 2** | Claim de grupos configurada + `EntraGroupRoleMapping` | `ENTRA-02` | Com 1-2 grupos de teste mapeados manualmente no banco, o callback consegue resolver `EmpresaId` + `Role` a partir do `groups` do token (ainda só logando o resultado, sem criar usuário). |
| **PR 3** | JIT completo — cria/sincroniza `Usuario`, emite JWT reaproveitando `IAuthService` | `ENTRA-04`, `ENTRA-05` | Primeiro login cria o usuário certo; segundo login sincroniza mudança de grupo; usuário sem grupo mapeado recebe 403 sem criar nada; usuário removido do grupo perde acesso sem ser excluído. |
| **PR 4** | Flag `authMethods` por Empresa + frontend | `ENTRA-06`, `ENTRA-07`, `ENTRA-08` | Empresa só-Local rejeita tentativa de Entra ID e vice-versa; botão "Entrar com Microsoft" funciona ponta a ponta no Vue. **Não pular esta PR** — sem ela, "manter os dois mundos" fica só como intenção, sem controle real por cliente. |
| **PR 5** | Testes de integração + documentação | `ENTRA-09`, `ENTRA-10`, `ENTRA-11` | Suite completa verde, incluindo o gate multi-tenant e o gate dos dois mundos, antes de considerar pronto pra produção. |

Estimativa total: ~10-11 dias úteis de engenharia, a maior parte concentrada nas PRs 3 e 5 (a lógica de sincronização e a garantia de que ela não fura o isolamento multi-tenant nem os dois mundos de autenticação).

---

## 7. Pendências que bloqueiam o início

1. **Aurora:** indicar quem administra o tenant Entra ID (registro de consentimento do app) e definir os grupos que mapeiam pra `EntraGroupRoleMapping`, com os Object IDs — não só os nomes. Sem isso, a PR 2 em diante fica bloqueada (a PR 1, login isolado, pode começar sem essa informação).
2. **Interno, não é sobre o Entra ID — é sobre onde a API roda hoje:** o deploy atual do backend é na Railway (`project_railway_deploy.md`), que **não tem região no Brasil** (confirmado: só Oregon, Ohio, Virginia, Frankfurt, Singapura). Isso é uma pendência da decisão de hospedagem Brasil (já discutida em outro momento, ver `Docs/rastreamento-motorista-backlog.md`), não do Entra ID em si — mas qualquer variável de ambiente nova (`EntraIdOptions:*`) vai, por ora, ainda ser configurada na Railway até essa migração de hospedagem acontecer. Não deixar essa pendência se perder de vista só porque o foco virou autenticação.

---

## 8. Como testar sem esperar a Aurora

Duas camadas de teste, cada uma cobrindo uma parte diferente do fluxo:

### 8.1 Lógica de provisionamento + tela somente-leitura — testável agora, zero dependência externa

`POST /v1/dev/seed-entra-login` (`DevToolsController.cs`) chama o `JitProvisioningService` de verdade, com claims fabricadas, simulando exatamente o que o callback real receberia do Entra ID. Bloqueado com `NotFound()` fora de `Development` — nunca existe em produção.

Passo a passo:
1. Escolher uma `Empresa` existente no seu banco local e rodar:
   ```sql
   UPDATE "Empresa" SET "Configuracoes" = '{"authMethods":["EntraId"]}' WHERE "Id" = '<empresa-id>';
   ```
2. Inserir um mapeamento de teste (via endpoint futuro de administração, ou direto no banco por enquanto):
   ```sql
   INSERT INTO "EntraGroupRoleMapping" ("Id","EmpresaId","EntraGroupId","EntraGroupNome","RoleName","Ativo","CreatedAt")
   VALUES (gen_random_uuid(), '<empresa-id>', 'grupo-teste-001', 'TruckFlow-Teste-Admin', 'Admin', true, now());
   ```
3. Chamar o endpoint (Swagger ou curl), rodando a API em `Development`:
   ```bash
   curl -X POST http://localhost:8080/v1/dev/seed-entra-login \
     -H "Content-Type: application/json" \
     -d '{"entraObjectId":"oid-fake-001","email":"teste.sso@aurora.com","nomeExibicao":"Usuário Teste SSO","entraGroupIds":["grupo-teste-001"]}'
   ```
4. Logar como um Admin dessa `Empresa` no painel web e abrir "Gerenciar Usuários" — o usuário criado aparece com o chip **SSO**, sem os botões de editar/ativar-inativar. Chamar `PATCH`/`DELETE` direto nesse usuário via Swagger deve retornar 400 (`BusinessException`) mesmo sem passar pela UI.
5. Repetir a chamada com `entraGroupIds: []` — o mesmo usuário deve ser desativado (`deletedAt` preenchido) sem ser excluído.

Isso testa `ENTRA-04`, `ENTRA-05`, `ENTRA-06` (parcialmente — a parte de rejeição por `authMethods`) e `ENTRA-08` de ponta a ponta, sem precisar de nenhuma credencial do Azure.

### 8.2 Fluxo OIDC real (login.microsoftonline.com, MFA, claim de grupos) — antes da Aurora existir

Não precisa esperar a Aurora pra testar o handshake real do Entra ID. O **Microsoft 365 Developer Program** (`developer.microsoft.com/microsoft-365/dev-program`) dá de graça um tenant Entra ID completo, com permissão de administrador:

1. Criar a conta de desenvolvedor (gratuita, e-mail comum serve).
2. No tenant de teste, criar 1-2 usuários e 1 grupo de segurança (ex.: `TruckFlow-Teste-Admin`), adicionar o usuário ao grupo.
3. Registrar o App Registration **nesse tenant de teste**, exatamente como descrito no `ENTRA-03` (multi-tenant, redirect URI, "Groups assigned to the application"). Preencher `EntraIdOptions:TenantId/ClientId/ClientSecret` locais (via `dotnet user-secrets`) com esses valores de teste.
4. Cadastrar o `EntraGroupRoleMapping` real (Object ID do grupo de teste, não mais o fake do item 8.1).
5. Clicar em "Entrar com Microsoft" no admin local — o fluxo completo roda de verdade: redirect, login Microsoft, (opcionalmente habilitar Conditional Access/MFA no tenant de teste também), volta autenticado, JIT cria o usuário, JWT emitido, dashboard abre.

**Por que isso não precisa refazer nada depois:** o App Registration é multi-tenant (`TenantId: "organizations"`). Quando a Aurora enviar o consentimento do tenant deles, é o **mesmo** app registration — só troca quem deu consentimento, nenhuma linha de código muda. O tenant de teste vira só mais um cliente que usa Entra ID, exatamente como a arquitetura já prevê.
