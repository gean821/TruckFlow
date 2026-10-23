# Política de Segurança da Informação (PSI) — TruckFlow

*Documento interno, versão 1.0 — [DATA]*

## 1. Objetivo

Estabelecer diretrizes básicas de segurança da informação para o desenvolvimento, operação e manutenção do sistema TruckFlow, garantindo confidencialidade, integridade e disponibilidade dos dados tratados (próprios, de clientes e de titulares de dados pessoais).

## 2. Escopo

Aplica-se a todos os componentes do sistema TruckFlow (backend, aplicativo mobile do motorista, painel administrativo web) e a todas as pessoas com acesso a esses ambientes (equipe de desenvolvimento).

## 3. Princípios gerais

- **Menor privilégio**: acesso a sistemas e dados é concedido apenas na medida necessária para a função exercida.
- **Segregação por cliente (multi-tenant)**: dados de uma empresa cliente nunca são acessíveis a outra, reforçado em múltiplas camadas técnicas (filtro de consulta + interceptor de escrita).
- **Defesa em profundidade**: controles de segurança são aplicados em mais de uma camada (aplicação, autenticação, infraestrutura).
- **Minimização de dados**: apenas dados pessoais necessários à operação do serviço são coletados e retidos.

## 4. Gestão de identidade e acesso

- Autenticação via usuário e senha, com política de senha mínima (10 caracteres, exigência de caractere não alfanumérico) e bloqueio de conta após tentativas de login malsucedidas.
- Autenticação multifator (MFA) disponível de forma opcional para usuários administrativos.
- Sessões controladas por tokens de acesso de curta duração, com renovação segura e revogação imediata em caso de logout ou suspeita de comprometimento.
- Papéis de acesso (Admin, Operador, Motorista) definem o que cada usuário pode visualizar ou modificar.

## 5. Proteção de dados

- Comunicação entre cliente e servidor sempre criptografada (HTTPS/TLS).
- Dados sensíveis não são expostos em mensagens de erro nem registrados em log em texto claro.
- Credenciais de infraestrutura (banco de dados, chaves de API) são armazenadas como variáveis de ambiente, nunca versionadas em código-fonte.
- Rotinas de backup diário com teste periódico de restauração.
- Toda exclusão completa de dados de uma empresa cliente (ex.: fim de contrato) gera um registro de auditoria formal e imutável, confirmando data, escopo e execução do evento, conforme esta política.
- Eventos de segurança (logs de aplicação e alertas de borda) são centralizados em ferramenta de monitoramento, com revisão mensal pelo responsável técnico.

## 6. Desenvolvimento seguro

- Todo código é revisado via Pull Request antes de ser incorporado ao sistema em produção.
- Build e testes automatizados são exigidos como pré-requisito para integração de código novo.
- Análise estática de código roda de forma automatizada a cada alteração.

## 6.1 Janelas de manutenção

Manutenções programadas que possam causar indisponibilidade são comunicadas aos clientes com no mínimo 24 horas de antecedência, informando data, horário estimado e canal de acompanhamento, através do canal de suporte da plataforma.

## 7. Resposta a incidentes

- Qualquer suspeita de incidente de segurança (vazamento de dados, acesso indevido, credencial exposta) deve ser reportada imediatamente ao responsável técnico.
- Credenciais comprometidas são revogadas e substituídas assim que identificadas.
- Em caso de incidente confirmado que afete dados de uma empresa cliente, o cliente será notificado em até **48 horas** após a confirmação, com informações preliminares sobre o ocorrido e as ações de contenção adotadas.

## 8. Revisão

Esta política é revisada de forma não periódica, conforme mudanças relevantes na arquitetura do sistema ou no volume de operação. Não há, no momento, ciclo formal de revisão nem aprovação por comitê de governança — é mantida e aplicada pelo responsável técnico do produto.

## 9. Responsável

Gean Luca Costa Ramos — responsável técnico e arquiteto do sistema TruckFlow.
