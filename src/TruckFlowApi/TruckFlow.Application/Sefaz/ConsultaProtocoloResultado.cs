namespace TruckFlow.Application.Sefaz
{
    public class ConsultaProtocoloResultado
    {
        public required string ChaveAcesso { get; init; }
        public required int CStat { get; init; }            // 100 = autorizada, 101 = cancelada, 110 = denegada, 217 = não consta...
        public required string XMotivo { get; init; }
        public string? Protocolo { get; init; }              // nProt
        public DateTime? DataAutorizacao { get; init; }      // dhRecbto
        public required int Ambiente { get; init; }          // 1 = Produção, 2 = Homologação
        public string? RawRespostaXml { get; init; }         // útil pra log/auditoria
        public bool Autorizada => CStat == 100;
        public bool Cancelada => CStat == 101;
        public bool Denegada  => CStat == 110;
    }
}
