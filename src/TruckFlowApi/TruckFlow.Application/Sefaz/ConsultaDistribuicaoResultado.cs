namespace TruckFlow.Application.Sefaz
{
    /// <summary>
    /// Resultado da consulta de distribuição de DF-e (NFeDistribuicaoDFe) por chave de acesso.
    /// Devolve o XML completo da nota (não só status) — por isso XmlNfe é string, não um
    /// objeto tipado da lib Zeus: mantém ISefazClient livre de tipos de terceiro, e o XML
    /// retornado reaproveita o mesmo parser usado no upload manual (NotaFiscalXmlExtractor).
    /// </summary>
    public class ConsultaDistribuicaoResultado
    {
        public required string ChaveAcesso { get; init; }
        public required int CStat { get; init; }
        public required string XMotivo { get; init; }
        public string? XmlNfe { get; init; }
        public bool Disponivel => !string.IsNullOrWhiteSpace(XmlNfe);
    }
}
