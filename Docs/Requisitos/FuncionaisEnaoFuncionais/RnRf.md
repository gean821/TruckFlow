# 📋 Requisitos do Sistema TruckFlow

## Requisitos Funcionais (RF)

🔹 RF01 – Registro de Empresa com Administrador

O sistema deve permitir o registro de uma nova empresa juntamente com um usuário administrador vinculado a essa empresa.

RNF associados ao RF01:

RNF01 – O sistema deve validar os dados da empresa utilizando FluentValidation.

RNF02 – O CNPJ deve ser único no sistema.

RNF03 – O processo de criação de empresa e administrador deve ocorrer de forma transacional.

RNF04 – A senha do administrador deve ser armazenada de forma criptografada via ASP.NET Identity.

RNF05 – O administrador deve receber a role "Admin".

RNF06 – O sistema deve registrar automaticamente a data de criação (CreatedAt).

RNF07 – A operação deve respeitar o padrão de arquitetura em camadas.

RNF08 – O tempo máximo de resposta deve ser inferior a 4 segundos.

RNF09 – O sistema deve impedir duplicidade de email.

RNF10 – O sistema deve impedir duplicidade de username.

🔹 RF02 – Registro de Administrador

O sistema deve permitir o cadastro de um novo administrador vinculado a uma empresa existente.

RNF associados:

RNF01 – Deve validar a existência da empresa antes da criação.

RNF02 – Deve utilizar criptografia segura para senha.

RNF03 – Deve garantir unicidade de email.

RNF04 – Deve garantir unicidade de username.

RNF05 – Deve atribuir automaticamente a role "Admin".

RNF06 – Deve registrar CreatedAt em UTC.

RNF07 – A operação deve ser assíncrona.

RNF08 – Deve utilizar injeção de dependência.

RNF09 – Deve retornar DTO e não entidade de domínio.

RNF10 – Deve lançar exceção estruturada em caso de erro.

RNF11 - O sistema deve impedir senhas criadas sem no mínimo 8 caracteres, um número, um carácter especial e uma letra maiúscula.

🔹 RF03 – Login de Administrador

O sistema deve permitir autenticação de administrador utilizando username ou email e senha válidos.

RNF associados:

RNF01 – Deve utilizar autenticação baseada em JWT.

RNF02 – O token deve conter claims de UserId e Role.

RNF03 – O token deve possuir tempo de expiração configurável.

RNF04 – Não deve permitir login de usuários com DeletedAt preenchido.

RNF05 – O sistema deve responder em até 4 segundos.

RNF06 – Senhas não devem ser armazenadas ou logadas em texto plano.

RNF07 – Deve impedir enumeração de usuários (mensagem de erro).

RNF08 – Deve utilizar HTTPS.

RNF09 – O token deve ser assinado digitalmente.

RNF10 – Deve registrar tentativas de login (auditoria futura).

🔹 RF04 – Atualização de Administrador

O sistema deve permitir que um administrador atualize dados cadastrais.

RNF associados:

RNF01 – Deve exigir autenticação e role Admin.

RNF02 – Deve validar unicidade de email.

RNF03 – Deve registrar UpdatedAt automaticamente.

RNF04 – Deve manter integridade referencial.

RNF05 – Deve retornar DTO atualizado.

RNF06 – Deve impedir alteração de Id.

RNF07 – Deve respeitar escopo da empresa (EmpresaId).

🔹 RF05 – Exclusão de Administrador

O sistema deve permitir exclusão de um usuário administrador.

RNF associados:

RNF01 – Não deve remover registro fisicamente.

RNF02 – Deve preencher DeletedAt com UTC.

RNF03 – Deve impedir login após exclusão.

RNF04 – Deve manter histórico de auditoria.

RNF05 – Deve respeitar autorização baseada em roles.

🔹 RF06 – Registro de Motorista

O sistema deve permitir cadastro de motorista com criação de usuário vinculado.

RNF associados:

RNF01 – Deve criar usuário do Motorista.

RNF02 – Deve atribuir automaticamente role "Motorista".

RNF03 – Deve criptografar senha.

RNF04 – Deve registrar CreatedAt.

RNF05 – Deve validar unicidade de email.

RNF06 – Deve garantir integridade relacional.

RNF07 – Deve utilizar include controlado para carregamento de dados.

RNF08 – Deve seguir padrão de classes DTO.

🔹 RF07 – Login de Motorista

O sistema deve permitir autenticação de motorista via username ou email.

RNF associados:

RNF01 – Deve validar senha via Identity.

RNF02 – Deve bloquear usuários com DeletedAt.

RNF03 – Deve retornar token JWT.

RNF04 – Deve carregar entidade Motorista associada.

RNF05 – Deve responder em até 4 segundos.

RNF06 – Token deve conter Role "Motorista" e UserId.

RNF07 - O sistema deve armazenar prioritariamente a placa e o modelo de veículo do motorista.

🔹 RF08 – Atualização de Motorista

O sistema deve permitir que o motorista autenticado atualize seus próprios dados.

RNF associados:

RNF01 – Deve exigir autenticação.

RNF02 – Deve validar claim UserId.

RNF03 – Deve atualizar o usuário e Motorista.

RNF04 – Deve registrar UpdatedAt.

RNF05 – Deve manter consistência transacional.

RNF06 – Não deve permitir alterar EmpresaId.

RNF07 - Deve manter histórico de auditoria.

🔹 RF09 – Exclusão de usuário do Motorista

O sistema deve permitir que o motorista exclua sua própria conta.

RNF associados:

RNF01 – Deve preencher DeletedAt.

RNF02 – Deve impedir login após exclusão.

RNF03 – Não deve remover dados fisicamente.

RNF04 – Deve manter histórico de agendamentos.

🔹 RF10 – Alteração de Senha

O sistema deve permitir que o usuário altere sua senha mediante validação da senha atual.

RNF associados:

RNF01 – Deve validar senha atual.

RNF02 – Deve aplicar política de complexidade.

RNF03 – Deve criptografar nova senha.

RNF04 – Deve invalidar tokens antigos após alteração.

RNF05 – Não deve armazenar senha em logs.

RNF06 – Deve retornar erro em falha.

🔹 RF11 – Consulta de Dados do Motorista Autenticado

O sistema deve permitir que o motorista consulte seus próprios dados.

RNF associados:

RNF01 – Deve exigir autenticação.

RNF02 – Deve utilizar claim UserId.

RNF03 – Deve retornar apenas dados do próprio usuário.

RNF04 – Deve respeitar escopo multiempresa.

RNF05 - Não pode permitir a visualização do seu próprio UserId.

🔹 RF12 – Consulta de Veículos do Motorista

O sistema deve permitir que o motorista visualize seu veículos cadastrados.

RNF associados:

RNF01 – Deve consultar via repositório específico.

RNF02 – Deve retornar lista mapeada para DTO.

RNF03 – Deve responder em até 4 segundos.

RNF04 – Não deve expor entidade interna.

🔹 RF13 – Controle de Acesso Baseado em Role

O sistema deve restringir acesso a endpoints conforme role (Admin, Motorista, entre outras).

RNF associados:

RNF01 – Deve utilizar atributo [Authorize].

RNF02 – Deve validar Role no token JWT.

RNF03 – Deve retornar HTTP 403 para acesso negado.

RNF04 – Deve impedir escalonamento de privilégio.

🔹 RF14 – Isolamento Multiempresa

O sistema deve garantir que usuários só possam acessar dados vinculados à sua EmpresaId.

RNF associados:

RNF01 – Todas as consultas devem considerar EmpresaId.

RNF02 – Não deve haver vazamento de dados entre empresas.

RNF03 – Deve aplicar filtros globais quando necessário.

RNF04 – Deve manter isolamento lógico no banco de dados.

🔹 RF15 — Cadastrar Produto

O sistema deve permitir cadastrar um produto com Nome associado a LocalDescargaId.

RNF associados:

RNF01 — Deve validar o DTO com FluentValidation antes de persistir.

RNF02 — Deve retornar HTTP 400 em caso de falha de validação.

RNF03 — Deve persistir de forma assíncrona e confirmar com SaveChangesAsync.

RNF04 — Deve aceitar e respeitar CancellationToken durante o processo.

🔹  RF16 — Validar Dados no Cadastro de Produto

O sistema deve validar Nome (obrigatório e mínimo 3 caracteres) ao cadastrar produto.

RNF associados:

RNF01 — Deve manter mensagens de validação padronizadas e consistentes.

RNF02 — Deve impedir gravação parcial quando a validação falhar.

RNF03 — Deve expor erros de validação de forma estruturada (ex.: lista de erros/campos).

🔹  RF17 — Consultar Produto por ID

O sistema deve permitir buscar um produto por id e retornar ProdutoResponse.

RNF associados:

RNF01 — Deve retornar HTTP 404 quando o produto não existir.

RNF02 — Deve executar consulta de forma assíncrona.

RNF03 — Deve evitar retornar a entidade diretamente (usar DTO/Response).

RNF04 — Deve aceitar CancellationToken na consulta.

🔹  RF18 — Listar Todos os Produtos

O sistema deve listar todos os produtos e retornar lista (inclusive vazia).

RNF associados:

RNF01 — Deve retornar lista vazia ao invés de null quando não houver registros.

RNF02 — Deve mapear resultado para DTO (ProdutoResponse) para não expor entidade diretamente.

RNF03 — Deve executar de forma assíncrona e aceitar CancellationToken.

🔹  RF19 — Atualizar Produto

O sistema deve permitir o usuário atualizar Nome e LocalDescargaId de um produto existente e registrar UpdatedAt.

RNF associados:

RNF01 — Deve validar o DTO de edição antes de aplicar alterações.

RNF02 — Deve retornar HTTP 404 se o Produto não existir.

RNF03 — Deve retornar HTTP 404 se o Local de Descarga não existir.

RNF04 — Deve registrar UpdatedAt em UTC para consistência.

RNF05 — Deve confirmar persistência com SaveChangesAsync (operação atômica do caso de uso).

🔹  RF20 — Excluir Produto

O sistema deve excluir um produto existente pelo id.

RNF associados:

RNF01 — Deve retornar HTTP 404 quando o produto não existir.

RNF02 — Deve retornar HTTP 204 (NoContent) quando excluir com sucesso.

RNF03 — Deve realizar a exclusão de forma assíncrona e confirmar com SaveChangesAsync.

RNF04 — Deve aceitar CancellationToken.

🔹  RF21 — Cadastrar Fornecedor

O sistema deve permitir cadastrar um fornecedor com Nome, CNPJ e opcionalmente, associar produtos via lista de IDs (ProdutoIds).

RNF associados:

RNF01 — Deve validar o DTO de criação com FluentValidation antes de persistir.

RNF02 — Deve retornar HTTP 400 em falha de validação, com mensagens claras por campo.

RNF03 — Deve normalizar o CNPJ removendo caracteres não numéricos antes de salvar.

RNF04 — Deve persistir de forma assíncrona e confirmar com SaveChangesAsync.

RNF05 — Deve aceitar e respeitar CancellationToken no fluxo completo.

🔹  RF22 — Impedir cadastro duplicado por CNPJ

O sistema deve impedir o cadastro de fornecedor quando já existir outro fornecedor com o mesmo CNPJ.

RNF associados:

RNF01 — Deve aplicar regra de negócio (BusinessException) ao detectar duplicidade.

RNF02 — Deve retornar erro padronizado (ex.: HTTP 409 Conflict ou 400, conforme padrão do projeto).

RNF03 — Deve comparar CNPJ usando o formato normalizado, evitando falsos negativos.

🔹  RF23 — Consultar fornecedor por ID

O sistema deve permitir buscar fornecedor pelo id.

RNF associados:

RNF01 — Deve retornar HTTP 404 quando o fornecedor não existir.

RNF02 — Deve executar consulta de forma assíncrona.

RNF03 — Deve retornar DTO e não a entidade diretamente.

RNF04 — Deve aceitar CancellationToken.

🔹  RF24 — Consultar fornecedor por CNPJ

O sistema deve permitir buscar fornecedor pelo CNPJ.

RNF associados:

RNF01 — Deve retornar HTTP 404 quando não existir fornecedor com o CNPJ informado.

RNF02 — Deve tratar CNPJ em formato consistente (normalizar no endpoint/serviço).

RNF03 — Deve executar consulta de forma assíncrona com CancellationToken.

🔹  RF25 — Listar todos os fornecedores

O sistema deve listar todos os fornecedores cadastrados.

RNF associados:

RNF01 — Deve retornar lista vazia quando não houver registros (nunca null).

RNF02 — Deve mapear para FornecedorResponse (não expor entidade diretamente).

RNF03 — Deve executar de forma assíncrona e aceitar CancellationToken.

🔹  RF26 — Atualizar fornecedor

O sistema deve permitir o usuário atualizar os dados do fornecedor pelo id (ex.: Nome, CNPJ e associação a outros produtos).

RNF associados:

RNF01 — Deve validar o DTO de atualização com FluentValidation.

RNF02 — Deve retornar HTTP 404 se o fornecedor não existir.

RNF03 — Deve persistir alterações de forma assíncrona e confirmar com SaveChangesAsync.

RNF04 — Deve aceitar CancellationToken.

🔹  RF27 — Excluir fornecedor

O sistema deve permitir excluir um fornecedor pelo id.

RNF associados:

RNF01 — Deve retornar HTTP 404 se o fornecedor não existir.

RNF02 — Deve retornar HTTP 204 (NoContent) ao excluir com sucesso.

RNF03 — Deve executar exclusão de forma assíncrona e confirmar com SaveChangesAsync.

RNF04 — Deve aceitar CancellationToken.

🔹  RF28 — Adicionar produto a um fornecedor

O sistema deve permitir associar um produto a um fornecedor pelos IDs (fornecedorId, produtoId).

RNF associados:

RNF01 — Deve retornar HTTP 404 se o fornecedor não existir.

RNF02 — Deve retornar HTTP 404 se o produto não existir.

RNF03 — Deve impedir associação duplicada (se já associado, retornar erro de regra de negócio).

RNF04 — Deve persistir a associação com SaveChangesAsync de forma assíncrona.

RNF05 — Deve aceitar CancellationToken.

🔹  RF29 — Remover produto de um fornecedor

O sistema deve permitir remover a associação de um produto com um fornecedor pelos IDs.

RNF associados:

RNF01 — Deve retornar HTTP 404 se o fornecedor não existir.

RNF02 — Deve retornar HTTP 404 se o produto não estiver associado ao fornecedor.

RNF03 — Deve retornar HTTP 204 (NoContent) ao remover com sucesso.

RNF04 — Deve persistir a remoção com SaveChangesAsync de forma assíncrona.

RNF05 — Deve aceitar CancellationToken.

🔹  RF30 — Consultar fornecedores associados a uma lista de produtos

O sistema deve permitir consultar fornecedores a partir de uma lista de produtosIds, retornando os fornecedores distintos associados a esses produtos.

RNF associados:

RNF01 — Deve validar entrada: não aceitar lista nula ou vazia (erro de parâmetro).

RNF02 — Deve retornar HTTP 404 se nenhum produto for encontrado para os IDs informados.

RNF03 — Deve retornar HTTP 404 se não existir fornecedor associado aos produtos informados.

RNF04 — Deve remover duplicidades (Distinct) garantindo consistência do retorno.

RNF05 — Deve executar de forma assíncrona e aceitar CancellationToken.

🔹  RF31 — Cadastrar Local de Descarga

O sistema deve permitir cadastrar um local de descarga com Nome.

RNF associados:

RNF01 — Deve registrar CreatedAt no momento da criação.

RNF02 — Deve impedir persistência caso a validação falhe.

🔹  RF32 — Listar Todos os Locais de Descarga

O sistema deve retornar todos os locais cadastrados, incluindo seus produtos associados (quando existirem).

RNF associados:

RNF01 — Deve retornar lista vazia quando não houver registros (nunca null).

RNF02 — Deve incluir produtos associados já mapeados no DTO.

RNF03 — Deve evitar carregamento desnecessário de dados.

🔹  RF33 — Consultar Local de Descarga por ID

O sistema deve permitir buscar um local de descarga pelo id.

RNF associados:

RNF01 — Deve retornar 404 quando o local não existir.

RNF02 — Deve incluir produtos associados no retorno (quando existirem).

🔹  RF34 — Atualizar Local de Descarga

O sistema deve permitir o usuário atualizar o Nome de um local de descarga existente.

RNF associados:

RNF01 — Deve registrar UpdatedAt no momento da atualização.

RNF02 — Deve retornar 404 quando o local não existir.

🔹  RF35 — Excluir Local de Descarga

O sistema deve permitir excluir um local de descarga pelo id.

RNF associados:

RNF01 — Deve retornar 404 quando o local não existir.

RNF02 — Deve garantir integridade referencial (não permitir exclusão se houver dependências críticas).

🔹  RF36 — Cadastrar Grade de Agendamento

O sistema deve permitir cadastrar uma Grade vinculada a Produto, Fornecedor e UnidadeEntrega, definindo:

- DataInicio, DataFim

- HoraInicial, HoraFinal

- IntervaloMinutos

- (opcional) DiasSemana

RNF associados:

RNF01 — Deve garantir coerência temporal: DataFim maior ou igual DataInicio e HoraInicial maior ou igual que HoraFinal.

RNF02 — Deve rejeitar IntervaloMinutos menor ou igual 0.

RNF03 — Deve manter DiasSemana em formato persistível e consistente.

🔹  RF37 — Gerar Slots (Agendamentos disponíveis) ao criar Grade

Ao criar uma Grade, o sistema deve gerar automaticamente slots de agendamento (entidade Agendamento) para cada intervalo dentro do período configurado, marcando-os como Disponível.

RNF associados:

RNF01 — Os slots gerados devem ter DataInicio/DataFim registrados em UTC.

RNF02 — A geração deve respeitar DiasSemana: somente dias permitidos recebem slots.

RNF03 — Deve garantir consistência: se não houver slots, não deve tentar inserir em lote.

RNF04 — Deve persistir os slots em operação de lote (AddRangeAsync) para eficiência.

🔹  RF38 — Listar Grades

O sistema deve permitir listar todas as grades cadastradas.

RNF associados:

RNF01 — Deve retornar lista vazia quando não houver registros.

RNF02 — Deve retornar dados agregados necessários ao app/admin (Produto, Fornecedor, UnidadeEntrega) via DTO.

🔹  RF39 — Consultar Grade por ID

O sistema deve permitir buscar uma grade pelo id.

RNF associados:

RNF01 — Deve retornar 404 quando a grade não existir.

🔹  RF40 — Atualizar Grade

O sistema deve permitir atualizar os dados de uma grade existente, incluindo:

- Produto / Fornecedor

- datas/horários

RNF associados:

RNF01 — Deve validar a existência de Produto e Fornecedor informados.

RNF02 — Deve registrar UpdatedAt em UTC.

RNF03 — Deve preservar integridade: mudanças em período/intervalo devem ser compatíveis com a estratégia do sistema para slots existentes.

🔹  RF41 — Excluir Grade

O sistema deve permitir excluir uma grade.

RNF associados:

RNF01 — Deve bloquear exclusão se existirem agendamentos ativos, em andamento ou finalizados vinculados à grade.

RNF02 — Deve retornar erro de regra de negócio padronizado quando houver bloqueio de grade (ex.: BusinessException).

🔹  RF42 — Disponibilizar janelas de agendamento geradas pela Grade

O sistema deve disponibilizar “janelas” de slots derivados da grade para que motoristas possam selecionar horários.

RNF associados:

RNF01 — Deve garantir consistência de disponibilidade (slots começam como Disponível).

RNF02 — Deve impedir dupla reserva do mesmo slot (precisa de controle de concorrência no Agendamento).

🔵 AGENDAMENTO — VISÃO MOTORISTA

🔹  RF43 — Listar horários disponíveis por fornecedor e data

O sistema deve permitir que o motorista consulte slots disponíveis para uma determinada Empresa em uma data específica.

RNF associados:

RNF01 — Deve considerar o intervalo completo do dia em UTC (00:00 até 23:59:59).

RNF02 — Deve retornar apenas slots com status Disponivel.

RNF03 — A consulta deve ser otimizada (evitar carregamento excessivo de relacionamentos).

🔹  RF44 — Reservar vaga (slot)

O motorista deve poder reservar um slot disponível vinculando:

- Nota Fiscal

- Tipo de veículo

- Placa

- Usuário (motorista)

RNF associados:

RNF01 — Deve impedir reserva se o status não for Disponivel.

RNF02 — Deve impedir reserva se a Nota Fiscal não pertencer a mesma Empresa da vaga.

RNF03 — Deve garantir integridade concorrente (evitar dupla reserva do mesmo slot).

RNF04 — Deve atualizar UpdatedAt em UTC.

RNF05 — Deve ser operação atômica (reserva + update no mesmo ciclo transacional).

🔹  RF45 — Listar meus agendamentos

O motorista deve poder consultar os Histórico de Agendamentos.

RNF associados:

RNF01 — Deve filtrar por UsuarioId.

RNF02 — Deve retornar informações consolidadas (Produto, Unidade de Emtrega, Status, Nota Fiscal).

RNF03 — Deve suportar grande volume de histórico (idealmente paginável no futuro).

🔴 AGENDAMENTO — VISÃO ADMIN

🔹  RF46 — Criar agendamento avulso

O admin pode criar um agendamento manual (sem Grade).

RNF associados:

RNF01 — Se não houver motorista, status deve iniciar como Disponivel.

RNF02 — Se houver motorista, status deve iniciar como Agendado.

RNF03 — Data de início deve ser futura.

🔹  RF47 — Consultar agendamentos com filtros

O admin pode consultar agendamentos por:

- intervalo de datas

- fornecedor

- Placa do Veículo

- Nome do motorista

RNF associados:

RNF01 — Deve validar que DataFim é maior que a DataInicio.

RNF02 — Quando não informado, deve assumir janela padrão (hoje até +7 dias).

RNF03 — Deve converter datas para UTC corretamente.

RNF04 — Consulta deve ser eficiente para dashboards.

RNF05 — Deve persistir os dados para possível auditoria.

🔹  RF48 — Registrar chegada (Check-in)

Admin pode registrar chegada do caminhão.

RNF associados:

RNF01 — Deve permitir transição apenas se estado permitir (PodeTransitarPara).

RNF02 — Deve atualizar UpdatedAt em UTC.

🔹  RF49 — Finalizar operação (Check-out)

Admin pode finalizar a operação de descarga.

RNF associados:

RNF01 — Deve respeitar fluxo de estados.

RNF02 — Deve impedir finalização se estado inválido.

🔹  RF50 — Cancelar agendamento

Admins e Motoristas pode cancelar agendamento.

RNF associados:

RNF01 — Deve respeitar máquina de estados.

RNF02 — Deve registrar alteração de status com UpdatedAt.

🔹  RF51 — Atualizar agendamento

Admin pode alterar datas e horários.

RNF associados:

RNF01 — Deve validar novo horário.

RNF02 — Deve atualizar UpdatedAt em UTC.

RNF03 — Deve manter consistência do status após edição.

🔹  RF52 — Excluir agendamento

Admin pode excluir agendamento.

RNF associados:

RNF01 — Não permitir exclusão se o status for de AgendamentoAtivo.

RNF02 — Exclusão deve ser bloqueada por regra de negócio quando necessário.

🔹  RF53 — Controlar transição de status via máquina de estados

O sistema deve permitir apenas transições válidas entre estados:

Exemplo:

- Disponível → Agendado

- Agendado → EmAndamento

- EmAndamento → Finalizado

- Agendado → Cancelado

RNF associados:

RNF01 — Todas as transições devem passar pelo método AlterarStatus.

RNF02 — Transições inválidas devem gerar exceção de regra de negócio.

RNF03 — Máquina de estados deve ser centralizada e consistente.

🔹 RF54 — Fazer parse do XML de NF-e

O sistema deve permitir receber um arquivo XML (NF-e) e extrair os dados principais para um DTO parseado:

- chave, número, série, emissão

- emitente/destinatário

- valores/peso/placa

RNF associados:

RNF01 — Deve aceitar nfeProc e NFe e rejeitar XML que não seja NF-e válida.

RNF02 — Deve registrar erro com trecho do XML de forma limitada (evitar logar XML completo).

RNF03 — Deve tolerar NF sem itens (log warning) mas manter comportamento consistente.

🔹  RF55 — Validar dados parseados antes de salvar

O sistema deve validar a NF parseada e seus itens antes de persistir.

RNF associados:

RNF01 — Chave de acesso deve ter 44 dígitos numéricos.

RNF02 — Data de emissão não pode ser futura.

RNF03 — Cada item deve ter quantidade e valores positivos e coerentes (ValorTotal = Quantidade * ValorUnitario).

🔹  RF56 — Salvar Nota Fiscal parseada

O sistema deve persistir uma NF parseada no banco, vinculando-a a um Fornecedor (empresa do emitente) e armazenando itens.

RNF associados:

RNF01 — Deve vincular a NF ao fornecedor identificado por CNPJ normalizado (somente dígitos).

RNF02 — Deve suportar “self-healing”: se fornecedor existir sem CNPJ, completar com o CNPJ da NF.

RNF03 — Deve registrar o usuário que enviou (UploadedByUserId) quando integrado ao JWT.

🔹  RF57 — Criar fornecedor automaticamente quando necessário

Se o fornecedor da NF não existir no sistema, o sistema deve criar automaticamente um novo fornecedor com base nos dados da NF.

RNF associados:

RNF01 — Deve criar fornecedor com CreatedAt em UTC.

RNF02 — Deve registrar evento em log (auditoria operacional) indicando criação automática.

🔹  RF58 — Aprender/atualizar mapeamento de produtos via EAN

Ao salvar uma NF, o sistema deve tentar “aprender” o EAN dos itens para melhorar o match com produtos do sistema.

RNF associados:

RNF01 — Deve evitar custo alto desnecessário.

RNF02 — Deve ser resiliente: falha no aprendizado não deve corromper o salvamento da NF (idealmente tratar como warning).

🔹  RF59 — Consultar Nota Fiscal por chave de acesso

O sistema deve permitir buscar uma NF pela chave de acesso.
 
RNF associados:

RNF01 — Deve retornar 400 para chave inválida (tamanho/formato).

RNF02 — Deve retornar 404 quando não existir NF com aquela chave.

RNF03 — Deve retornar itens no DTO, mantendo consistência do formato.

🔹  RF60 — Validar que a NF pertence à empresa correta para permitir agendamento

Para o motorista conseguir reservar/concluir um agendamento, o sistema deve validar que a Nota Fiscal informada pertence à empresa correta (empresaId), ou seja:

a NF deve estar vinculada a EmpresaId (empresa) correspondente ao agendamento.

RNF associados:

RNF01 — A validação deve ser obrigatória e bloquear o fluxo quando houver divergência (erro de negócio padronizado).

RNF02 — O vínculo deve ser feito por identificador confiável (FornecedorId derivado do CNPJ do emitente).

RNF03 — A mensagem de erro deve ser clara (“NF não pertence a esta empresa”).