using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class DispositivoUsuario : EntidadeBase
    {
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public required string ExpoPushToken { get; set; }

        public PlataformaDispositivo Plataforma { get; set; }

        public string? AppVersion { get; set; }

        public DateTime UltimoUsoEm { get; set; }

        public bool Ativo { get; set; } = true;

        public void RegistrarUso()
        {
            UltimoUsoEm = DateTime.UtcNow;
            Ativo = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Desativar()
        {
            Ativo = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void TransferirPara(Guid novoUsuarioId)
        {
            UsuarioId = novoUsuarioId;
            UltimoUsoEm = DateTime.UtcNow;
            Ativo = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}