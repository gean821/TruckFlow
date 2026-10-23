# Plano de Continuidade de Negócios (PCN) e Recuperação de Desastres (PRD) — TruckFlow

*Documento interno, versão 1.0 — [DATA]*

## 1. Objetivo

Garantir a continuidade da operação da plataforma TruckFlow e a recuperação dos dados em caso de falha, incidente ou desastre que afete a infraestrutura de produção.

## 2. Objetivos de recuperação

- **RPO (Recovery Point Objective):** até 5 minutos de perda de dados no pior cenário.
- **RTO (Recovery Time Objective):** até 30 minutos para restauração do serviço.

## 3. Estratégia de backup e recuperação

- Backup contínuo e cópias diárias completas do banco de dados de produção, armazenados de forma segregada da infraestrutura principal.
- Testes mensais de restauração, com data e resultado documentados, validando que os backups são íntegros e restauráveis dentro dos objetivos de RPO/RTO definidos.
- Caso um teste de restauração identifique falha ou inconsistência, a causa é investigada e a ação corretiva correspondente é registrada antes do próximo teste programado.

## 4. Responsabilidades

- O responsável técnico do produto (Gean Luca Costa Ramos) é o responsável por acionar o plano, coordenar a restauração e comunicar o status às partes interessadas em caso de incidente.

## 5. Cenários cobertos

- Corrupção ou perda de dados no banco de produção.
- Indisponibilidade do provedor de infraestrutura/hospedagem.
- Erro humano ou falha de deploy que comprometa o ambiente de produção.

## 6. Comunicação durante um evento de continuidade

Em caso de indisponibilidade prolongada, os clientes afetados são comunicados conforme o processo de notificação de incidentes descrito na Política de Segurança da Informação (compromisso de notificação em até 48 horas).

## 7. Revisão e testes

Este plano é revisado sempre que houver mudança relevante na infraestrutura, e os testes de restauração são executados mensalmente como parte da rotina operacional.
