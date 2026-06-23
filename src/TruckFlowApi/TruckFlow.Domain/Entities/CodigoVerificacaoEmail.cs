using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class CodigoVerificacaoEmail
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public required string CodigoHash { get; set; }

        public FinalidadeVerificacaoEmail Finalidade { get; set; }

        public int Tentativas { get; set; } = 0;

        public required DateTime ExpiraEm { get; set; }

        public DateTime? UsadoEm { get; set; }

        public required DateTime CriadoEm { get; set; }

        public bool EstaAtivo(DateTime agoraUtc) =>
            UsadoEm is null && agoraUtc < ExpiraEm && Tentativas < 3;
    }
}
