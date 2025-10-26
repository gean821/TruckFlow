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


⚙️ Como Executar
Backend

# Clone o repositório
git clone [url-do-repositorio]

# Navegue até a pasta do backend
cd TruckFlow/Backend

# Restaure as dependências
dotnet restore

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
        


