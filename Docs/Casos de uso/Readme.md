# 📄 Documento de Casos de Uso – TruckFlow
#Atenção, o sistema nunca será um ator de caso de uso, ao fazer não inclua ele como um ator, e sim a validação,etc como parte do processo
---

## Aplivativo: MOTORISTA ##

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

## UC03 – Notificação de Imprevistos

**Atores Principal:** Motorista 
**Atores Secudários:** Administrador 

**Pré-condições:**
- Alterações no agendamento devido a imprevistos com a carga ou motorista.

**Fluxo Principal:**
1. O motorista por meio da tela de notificações informa o gestor do imprevisto.
2. O administrador visualiza a notificação e notifica o motorista caso haja realteração na carga.
3. O sistema envia mensagens automáticas para os motoristas afetados, caso necesário já reenvia sua nova programção de agendamento de descarga.

**Pós-condições:**
- Motoristas recebem informações atualizadas com programação já reajustada.

---

## UC04 – Visualização de Agendamento

**Atores Principais:** Motorista

**Pré-condições:**
- Usuário deve estar autenticado.

**Fluxo Principal:**
1. O usuário acessa a tela **"Meus Agendamentos"**.
2. O sistema exibe todos os agendamentos ativos e históricos.
3. O usuário pode filtrar e ordenar por data, fornecedor ou status.

**Pós-condições:**
- O usuário tem acesso rápido às informações de suas cargas.

--- 

## Aplicação Web: ADMINISTRADOR ##

## UC01 – Tela de Cadastro 

**Atores Principais:** Administrador

**Pré-condições:**
- Usuário deve conter um token para criar sua conta dentro do sistema.

**Fluxo Principal:**
1. O usuário acessa a tela **"CADASTRAR"**.
2. O sistema exige o token de criação da conta com todas as informações e acessos que seu sistema deve conter.
3. O Administrador deverá apenas colocar suas informações sendo elas nome de usuário, senha, telefone.

**Pós-condições:**
- O usuário terá acesso ao sistema com permissões já validadas pelo seu token de criação.

---

## UC02 – Tela de Login

*Atores Principais:* Administrador

*Pré-condições:*
- O Administrador deve contér um login já criado.

*Fluxo Principal:*
1. O usuário acessa a tela *"LOGIN"*.
2. O usuário informa suas informações de acesso.

*Pós-condições:*
- Usuário acessa o sistema e é liberado as funções para uso do mesmo.

---

## UC03 – Tela de Visualização

*Atores Principais:* Administrador

*Pré-condições:*
- 

*Fluxo Principal:*
1. O usuário acessa a tela *"Visualizar"*.
2. Na tela o mesmo pode visualizar todos os agendamentos, já em trânsito criado pelo motorista.
3. É mostrado para o usuário, a hora, o produto, o fornecedor, a placa e o peso da carga de cada agendamento.
4. Ao lado de cada agendamento é possível marcar uma check box para caso o caminhão já esteja dentro da unidade aguardando descarga.

*Pós-condições:*
- Fácil acesso a monitoração de caminhões à caminho da unidade.  

---
## UC04 – Tela de Programação

**Atores Principais:** Administrador 

**Pré-condições:**
- O administrador deve estar autenticado no sistema e seu usuário deverá ter permissão de acesso a esta tela.

**Fluxo Principal:**
1. O administrador acessa a tela *"Programação"*.
2. O sistema exibe uma tela para cadastro de programação de grades de agendamentos, que serão vizualizadas pelo motorista dentro do aplicativo.
3. O administrador faz a progrmação de acordo com filtros de fornecedor, produto, data de início, data de fim, hora e intervalo entra grades a serem criadas.
4. O sistema salva as programações e libera os agendamentos a serem escolhidas pelos motoristas.

---

## UC05 – Tela de Bloqueios

*Atores Principais:* Administrador

*Pré-condições:*
- O administrador deve estar autenticado no sistema e seu usuário deverá ter permissão de acesso a esta tela.

*Fluxo Principal:*
1. O usuário acessa a tela *"BLOQUEIOS"*.
2. O sistema será exibida uma tabela com todos os bloqueios feitos.
3. Terá como opção editar estes bloqueios e fazer a liberação dos mesmos ou a exclusão permanente deles (Será excluida a grade).
4. Se por acaso não tiver nenhum bloqueio feito, terá como opção a adição de novos bloqueios.
5. A tela de Adicionar Bloqueios terá como função gerar bloqueios de grades já criadas.
6. O usuário poderá fazer os bloqueios a partir de suas datas ou por grades já existentes.
7. Todos as grade dentro do filtro das datas, serão bloqueadas na sessão de bloqueio por datas.
8. Somente as grades selecionadas, serão bloqueadas na sessão de bloqueio por grades.

*Pós-condições:*
- Os bloqueios são feitos a partir da data e horários das grades, caso o bloqueio esteja ativo e foi excedido o horário da grade, ele será excluído altomaticamente.

---

## UC06 – Tela de Notificações

*Atores Principais:* Administrador

*Pré-condições:*
- O administrador deve estar autenticado no sistema e seu usuário deverá ter permissão de acesso a esta tela.

*Fluxo Principal:*
1. O usuário acessa a tela *"NOTIFICAÇÕES"*.
2. O sistema fornecerá opções entre 3 telas distintas sendo elas: Reagendar Carga, Notificações Motoristas e Enviar Notificação.
3. Reagendar Carga: A tela fornecerá uma série de filtros para seleção de cargas que serão reagendadas podendo ser filtradas a partir do fornecedo, produto, placa ou nome do motorista.
4. O reagendamento será feito a partir de grades disponíveis para aquela determinada carga.
5. Notificações Motorista: Toda notificação enviada por um motorista será possível visualizar dentro desta tela, tendo como opção responder ou reagendar direto a carga do mesmo.
6. Enviar Notificação: Terá como função enviar notificações para motoristas sendo possível somente enviar uma mensagem ou enviar a mensagem e já reagenda-lo.

*Pós-condições:*
- Após o reagendamento ser eftuado, será enviada uma mensagem automática via WhatsApp para o motorista com as infromações do novo agendamento.

---

## UC07 – Tela de Gereciamento 

*Atores Principais:* Administrador

*Pré-condições:*
- O administrador deve estar autenticado no sistema e seu usuário deverá ter permissão de acesso a esta tela.

*Fluxo Principal:*
1. O usuário acessa a tela *"GERENCIAR"*.
2. O sistema fornecerá opções entre 3 telas distintas sendo elas: Cadastrar, Gerenciar Recebimento e Visualizar Recebimento.
3. Cadastrar: Será oferecido ao usuário telas para cadastros de fornecedores, produtos, local de descargas (será associado a um produto para criação de grades distintas) e edição de cadastros.
4. Gerenciar Recebimento: Tela onde o usuário poderá programar a quantidade de cada produto a ser entregue dentro de uma determinada data, fazendo com que os motoristas não possam criar agendamentos excedendo a quantidade estipulada. 
5. Controle de Recebimento: Será uma tabela gerada para vizualizar e fazer o acompanhamento dos produtos de acordo com a programação de recebimento.
6. Poderá ser feito a edição dos Recebimentos já criados, podendo editar somente a quantidade a ser entregue ou a exclusão do recebimento.

---

## UC08 – Tela de Relatórios

*Atores Principais:* Administrador

*Pré-condições:*
- O administrador deve estar autenticado no sistema e seu usuário deverá ter permissão de acesso a esta tela.

*Fluxo Principal:*
1. O usuário acessa a tela *"RELATÓRIOS"*.
2. Será gerado relatórios de recebimento sendo criados a partir de uma série de filtros escolhidos pelo usuários.
3. Filtros: Fornecedor, Produto, Placa do caminhão, Nome do motorista, Data Início, Data Fim e Hora.
4. Estes relatórios poderão ser gerados em arquivos PDF ou em Excel de acordo com a necessidade do usuário. 

---