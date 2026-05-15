TruckFlow - Sistema de Gestão de Descarga de Matéria Prima
🎯 Sobre o Projeto
TruckFlow é uma solução completa para gerenciamento e otimização do processo de descarga de matéria prima em indústrias. O sistema foi desenvolvido para resolver problemas comuns como congestionamento no pátio, ineficiência no agendamento e dependência de processos manuais em planilhas.

📱 Funcionalidades Principais
Aplicativo Mobile (Motoristas)

Agendamento digital de descargas
Leitura automática de notas fiscais (PDF/código)
Visualização de horários disponíveis
Acompanhamento do status do agendamento
Notificações em tempo real
Interface Web (Administradores)

Gestão de horários disponíveis
Dashboard de agendamentos
Controle de fluxo de caminhões
Relatórios e métricas
Gestão de usuários e permissões


🚀 Stack Tecnológica
Backend
.NET 6.0+
Entity Framework Core
SQL Server
Clean Architecture
Docker


Frontend
Vue.js 3
Vuetify 3
Pinia (Gerenciamento de Estado)
Vue Router
Axios
TypeScript

🏗️ Arquitetura do Projeto

TruckFlow/
├── Backend/
│   ├── TruckFlow.Domain/        # Entidades e regras de negócio
│   ├── TruckFlow.Application/   # Casos de uso
│   ├── TruckFlow.Infrastructure/# Persistência e serviços
│   └── TruckFlow.API/          # Controllers e configurações
└── Frontend/
    └── truckflow-web/          # Interface administrativa (Vue.js)
        ├── src/
        │   ├── components/      # Componentes Vue
        │   ├── views/          # Páginas da aplicação
        │   ├── store/          # Gerenciamento de estado (Pinia)
        │   ├── router/         # Configuração de rotas
        │   └── services/       # Serviços e APIs


🔐 Configuração de Secrets (Desenvolvimento)

Credenciais (connection string do banco, chave de assinatura JWT) **não são versionadas** —
ficam no `dotnet user-secrets` por máquina. Quando você clonar o repo pela primeira vez,
o `appsettings.json` está com os campos vazios; rode os comandos abaixo para popular
seus secrets locais.

> `UserSecretsId` já está configurado no `TruckFlow.csproj` — não precisa rodar `init`.

**1. Setar a connection string do Postgres local**

```bash
cd src/TruckFlowApi/TruckFlow

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=TruckFlow;Username=truckflow;Password=<SUA_SENHA_LOCAL>"
```

**2. Gerar uma chave JWT crypto-segura (512-bit, Base64)**

Windows PowerShell 5.1:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 64
$rng.GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

PowerShell 7+ / pwsh:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

Linux/macOS:

```bash
openssl rand -base64 64 | tr -d '\n'
```

Copie a saída e salve no user-secrets:

```bash
dotnet user-secrets set "JwtOptions:SecurityKey" "<chave_base64_gerada>"
```

**3. Conferir**

```bash
dotnet user-secrets list --project src/TruckFlowApi/TruckFlow/TruckFlow.csproj
```

Saída esperada:

```
ConnectionStrings:DefaultConnection = Host=localhost;...
JwtOptions:SecurityKey = <chave_base64>
```

**Onde fica fisicamente o arquivo** (fora do repo, no perfil do usuário):

- Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

**Comandos úteis:**

```bash
# Remover uma chave específica
dotnet user-secrets remove "JwtOptions:SecurityKey" --project src/TruckFlowApi/TruckFlow/TruckFlow.csproj

# Limpar todos
dotnet user-secrets clear --project src/TruckFlowApi/TruckFlow/TruckFlow.csproj
```

> ⚠️ **Atenção:** user-secrets só é carregado quando `ASPNETCORE_ENVIRONMENT=Development`.
> O `launchSettings.json` do projeto já define isso automaticamente — só fique atento
> se for rodar com `--no-launch-profile` ou em outro ambiente.

> 🚀 **Produção:** em produção (Railway, AWS, Azure, etc.), os secrets viram **environment
> variables** com `__` (dois underscores) substituindo o `:` — ex.: `ConnectionStrings__DefaultConnection`
> e `JwtOptions__SecurityKey`. **Não** suba `secrets.json` para servidor.

---

⚙️ Como Executar
Backend

# Clone o repositório
git clone [url-do-repositorio]

# Navegue até a pasta do backend
cd TruckFlow/Backend

# Restaure as dependências
dotnet restore

# Configure os secrets locais (ver seção "🔐 Configuração de Secrets" acima)

# Execute as migrações
dotnet ef database update

# Inicie a API
dotnet run --project TruckFlow.API



Frontend

# Navegue até a pasta do frontend
cd Frontend/truckflow-web

# Instale as dependências
npm install

# Execute em modo desenvolvimento
npm run dev

# Build para produção
npm run build


💻 Funcionalidades do Sistema Web
Painel Administrativo
Gestão de horários disponíveis
Dashboard com métricas em tempo real
Visualização de agendamentos
Relatórios gerenciais
Gestão de usuários e permissões
Operacional
Confirmação de chegada de caminhões
Gestão de docas
Acompanhamento de descargas
Histórico de operações
        


