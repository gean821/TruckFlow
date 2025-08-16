# 📋 Requisitos do Sistema TruckFlow

## ✅ Requisitos Funcionais (RF)

| **ID** | **Requisito Funcional** | **Descrição** |
|--------|------------------------|---------------|
| RF01 | Cadastro de caminhões | Permitir que transportadores cadastrem informações sobre seus caminhões, incluindo placa, |tipo e capacidade de carga. |

| RF02 | Cadastro de motoristas | Possibilitar o registro de motoristas, incluindo nome, CNH e empresa associada. |

| RF03 | Agendamento de descarga | Permitir que usuários autorizados realizem agendamentos para descarga de caminhões em horários e datas específicas. |

| RF04 | Controle de janelas de agendamento | Disponibilizar a visualização e gerenciamento de horários disponíveis para descarga. |

| RF05 | Confirmação e cancelamento de agendamento | Permitir que o usuário confirme ou cancele um agendamento previamente feito. |

| RF06 | Painel administrativo | Disponibilizar um painel para a gestão de todos os agendamentos, incluindo status, horários e caminhões associados. |

| RF07 | Notificações automáticas | Enviar notificações (e-mail ou SMS) para confirmar, lembrar ou alterar agendamentos. |

| RF08 | Relatórios | Gerar relatórios sobre movimentações, agendamentos e desempenho logístico. |

| RF09 | Login e autenticação | Garantir acesso seguro ao sistema, com níveis de permissão (administrador, operador, transportador). |

| RF10 | Histórico de agendamentos | Armazenar e disponibilizar o histórico de agendamentos realizados para consulta posterior. |

---

## ⚙️ Requisitos Não Funcionais (RNF)

| **ID** | **Requisito Não Funcional** | **Descrição** |
|--------|----------------------------|---------------|
| RNF01 | Disponibilidade | O sistema deve estar disponível 99,5% do tempo, garantindo operação contínua. |

| RNF02 | Performance | O carregamento das páginas e consultas deve ocorrer em até 2 segundos em conexões médias (10 Mbps). |

| RNF03 | Segurança | Todas as comunicações devem ser criptografadas via HTTPS e senhas armazenadas com hash seguro (ex.: bcrypt). |

| RNF04 | Escalabilidade | O sistema deve suportar aumento de usuários simultâneos sem degradação significativa de performance. |

| RNF05 | Portabilidade | A aplicação deve ser acessível por navegadores modernos (Chrome, Firefox, Edge) e 
dispositivos móveis. |

| RNF06 | Usabilidade | A interface deve ser intuitiva e seguir padrões de design responsivo. |

| RNF07 | Conformidade | O sistema deve atender à LGPD (Lei Geral de Proteção de Dados) para tratamento de dados pessoais. |

| RNF8 | Manutenibilidade | O código deve seguir boas práticas de desenvolvimento e estar documentado para facilitar futuras alterações. |
