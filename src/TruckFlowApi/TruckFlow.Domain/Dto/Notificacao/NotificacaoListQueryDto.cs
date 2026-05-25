namespace TruckFlow.Domain.Dto.Notificacao
{
    public class NotificacaoListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public bool? UnreadOnly { get; set; }
        public int? Tipo { get; set; }
        public int? Prioridade { get; set; }
    }
}
