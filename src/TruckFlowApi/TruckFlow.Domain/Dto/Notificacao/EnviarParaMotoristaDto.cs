namespace TruckFlow.Domain.Dto.Notificacao
{
    public class EnviarParaMotoristaDto
    {
        public Guid AgendamentoId { get; set; }
        public string? Titulo { get; set; }
        public required string Corpo { get; set; }
    }
}