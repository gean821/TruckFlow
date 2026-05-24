namespace TruckFlow.Domain.Dto.Notificacao
{
    public class EnviarParaEmpresaDto
    {
        public Guid AgendamentoId { get; set; }
        public string? Titulo { get; set; }
        public required string Corpo { get; set; }
    }
}