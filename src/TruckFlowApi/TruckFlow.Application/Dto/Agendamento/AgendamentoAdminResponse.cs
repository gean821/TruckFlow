using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TruckFlow.Application.Dto.Agendamento
    {
        //esse response será o responsável por mostrar os dados para o MOTORISTA no app mobile. Há dois tipos:
        //1- Response de agendamento do Motorista no app
        //2- Response de agendamento para o Admin no sistema.
        public sealed class AgendamentoAdminResponse
        {
            public required Guid Id { get; set; }

            public required Guid FornecedorId { get; set; }
            public required Guid TipoCargaId { get; set; }
            public required Guid MotoristaId { get; set; }
            public required DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public DateTime? DeletedAt { get; set; }
        }
    }
