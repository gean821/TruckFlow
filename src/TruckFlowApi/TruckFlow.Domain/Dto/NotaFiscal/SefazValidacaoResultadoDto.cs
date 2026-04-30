namespace TruckFlow.Domain.Dto.NotaFiscal
{
    public class SefazValidacaoResultadoDto
    {
        public required string ChaveAcesso { get; init; }
        public required int CStat { get; init; }
        public required string XMotivo { get; init; }
        public string? Protocolo { get; init; }
        public DateTime? DataAutorizacao { get; init; }
        public required int Ambiente { get; init; }
        public required bool Autorizada { get; init; }
        public required bool NotaPersistidaAtualizada { get; init; }   // se a nota existia no banco e foi atualizada
        public required DateTime ValidadaEm { get; init; }
    }
}
