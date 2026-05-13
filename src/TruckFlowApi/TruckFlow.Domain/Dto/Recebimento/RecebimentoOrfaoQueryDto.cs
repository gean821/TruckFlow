namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoOrfaoQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
    }
}
