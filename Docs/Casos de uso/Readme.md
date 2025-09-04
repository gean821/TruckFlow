# 📄 Documento de Casos de Uso – TruckFlow
#Atenção, o sistema nunca será um ator de caso de uso, ao fazer não inclua ele como um ator, e sim a validação,etc como parte do processo
---

## UC01 – Cadastro de Motorista

**Atores Principais:** Motorista  

**Pré-condições:**
- O motorista deve ter acesso ao aplicativo.
- O motorista só poderá acessar a funcionalidade de agendamento após concluir o cadastro.

**Fluxo Principal:**
1. O motorista acessa o aplicativo e seleciona a opção **"Cadastro"**.
2. O sistema exibe um formulário solicitando dados pessoais e do veículo.
3. O motorista preenche todos os campos obrigatórios: nome, telefone, tipo do caminhão, placa, fornecedor, unidade de entrega, nota fiscal.
4. O sistema valida as informações e salva o cadastro.
5. O sistema exibe mensagem de sucesso e redireciona para a tela inicial.

**Fluxo Alternativo:**
- No passo 3, caso algum campo obrigatório não seja preenchido, o sistema indica o campo ausente e impede a conclusão até que seja preenchido corretamente.

**Pós-condições:**
- O cadastro do motorista é armazenado no sistema e listado para visualização dos administradores.

---

## UC02 – Agendamento de Carga

**Atores Principais:** Motorista  
**Atores Secundários:** Administrador (pode criar agendamentos manualmente, se necessário)  

**Pré-condições:**
- O motorista deve estar cadastrado no sistema.
- Deve haver horários e fornecedores disponíveis definidos pelo gestor.

**Fluxo Principal:**
1. O motorista acessa o aplicativo e seleciona **"Agendar Carga"**.
2. O sistema exibe lista de fornecedores e horários disponíveis.
3. O motorista seleciona o fornecedor e horário desejado.
4. O sistema valida disponibilidade e confirma o agendamento.
5. O sistema exibe mensagem de sucesso e disponibiliza visualização do agendamento.

**Fluxo Alternativo:**
- Se o horário selecionado já estiver indisponível, o sistema exibe mensagem e solicita nova escolha.

**Pós-condições:**
- O agendamento é registrado e vinculado ao motorista.

---

## UC03 – Edição de Programação (Gestor)

**Atores Principais:** Administrador/Gestor  

**Pré-condições:**
- O administrador deve estar autenticado no sistema.

**Fluxo Principal:**
1. O administrador acessa o painel de controle.
2. O sistema exibe a programação semanal de descarregamentos.
3. O administrador edita dados como fornecedor, horário, tipo e volume da carga.
4. O sistema salva as alterações e atualiza os agendamentos impactados.

**Fluxo Alternativo:**
- Se a alteração afetar agendamentos existentes, o sistema solicita confirmação e gera notificações aos motoristas afetados.

**Pós-condições:**
- A programação é atualizada e todos os usuários recebem as mudanças aplicáveis.

---

## UC04 – Notificação de Alterações

**Atores Principais:** Administrador/Gestor  
**Atores Secundários:** Motorista  

**Pré-condições:**
- Alterações no agendamento ou programação devem ter sido realizadas.

**Fluxo Principal:**
1. O sistema identifica mudanças nos horários ou cargas.
2. O administrador confirma o envio das notificações.
3. O sistema envia mensagens automáticas para os motoristas afetados.

**Pós-condições:**
- Motoristas recebem informações atualizadas para ajustar sua programação.

---

## UC05 – Comunicação de Imprevistos

**Atores Principais:** Motorista  
**Atores Secundários:** Administrador/Gestor  

**Pré-condições:**
- O motorista deve estar cadastrado no sistema.
- Deve existir um agendamento ativo vinculado a ele.

**Fluxo Principal:**
1. O motorista acessa a opção **"Reportar Problema"** no aplicativo.
2. O sistema exibe opções como atraso, problema mecânico ou sinistro.
3. O motorista seleciona o tipo de problema e insere informações adicionais.
4. O sistema envia o aviso ao administrador.
5. O administrador analisa e, se necessário, oferece novos horários.

**Pós-condições:**
- O gestor é informado rapidamente e pode reagendar a carga.

---

## UC06 – Visualização de Agendamentos

**Atores Principais:** Motorista, Administrador  

**Pré-condições:**
- Usuário deve estar autenticado.

**Fluxo Principal:**
1. O usuário acessa a tela **"Meus Agendamentos"**.
2. O sistema exibe todos os agendamentos ativos e históricos.
3. O usuário pode filtrar e ordenar por data, fornecedor ou status.

**Pós-condições:**
- O usuário tem acesso rápido às informações de suas cargas.
