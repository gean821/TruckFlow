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


