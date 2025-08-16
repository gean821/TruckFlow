# 📦 Sistema de Agendamento de Caminhões – Fábrica de Ração

## 📌 1. Contexto
- **Empresa:** Fábrica de ração com **10 anos de operação**.  
- **Demanda:** Recebimento constante de matéria-prima para produção, com **pouco espaço de armazenagem**.  
- **Operação atual:**  
  - Agendamento e programação de recebimentos feitos via **planilha**.  
  - Comunicação individual com fornecedores e motoristas por telefone/mensagem.  
  - Caminhões aguardam no pátio, sendo necessário **evitar filas e veículos parados no meio da fábrica**.  

---

## 🚩 2. Problemas Identificados
- **Baixa automação** no processo de agendamento.  
- **Falta de centralização** da comunicação com motoristas.  
- **Dependência de contato manual** para avisos e alterações de horários.  
- **Dificuldade de organização logística** devido ao espaço limitado.  
- **Ausência de ferramenta simples** e adaptada ao perfil dos motoristas (público com pouca familiaridade tecnológica).  

---

## 💡 3. Requisitos e Funcionalidades do Novo Sistema

### 3.1 Aplicativo para Motoristas
- **Cadastro do motorista**:
  - Nome completo.
  - Telefone.
  - Placa e modelo do caminhão.
  - Nota fiscal da carga.
- **Agendamento da descarga**:
  - Seleção do fornecedor carregador e unidade de descarga.
  - Exibição apenas das empresas previamente cadastradas pelo gestor.
- **Comunicação direta**:
  - Receber notificações automáticas sobre alterações de horários ou problemas.
  - Possibilidade de informar imprevistos (problemas mecânicos, sinistros, atrasos).
- Interface **simples, intuitiva e funcional**, adaptada ao público-alvo.

### 3.2 Painel para Gestor/Administrador
- Criar e gerenciar **programação semanal** de descarregamentos.
- Definir **fornecedores** e **volumes de matéria-prima** para cada período.
- **Editar informações da carga**:
  - Data e hora de chegada.
  - Tipo da carga.
  - Placa do veículo.
  - Dados do motorista.
- **Controle de até 60 caminhões por dia**, operação 24h.
- Sistema **customizável** para atender múltiplas empresas (não engessado).

---

## 🔄 4. Fluxo Operacional Esperado
1. **Gestor** define programação e limites de recebimento.  
2. **Motorista** realiza cadastro e agenda horário disponível.  
3. Sistema valida disponibilidade e confirma agendamento.  
4. Em caso de alterações:
   - Gestor envia notificação automática para motoristas afetados.
   - Motorista pode sinalizar problemas e reagendar horário.
5. Monitoramento e ajustes feitos pelo gestor em tempo real.

---

## 🎯 5. Benefícios Esperados
- Eliminação de filas e caminhões parados indevidamente.  
- Redução da comunicação manual e de erros de agendamento.  
- Melhoria na previsibilidade e organização logística.  
- Maior controle operacional com histórico e rastreabilidade.  
- Sistema escalável para uso por outras empresas.  
