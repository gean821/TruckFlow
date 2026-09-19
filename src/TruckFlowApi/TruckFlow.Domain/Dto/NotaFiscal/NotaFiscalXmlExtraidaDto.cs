using System;
using System.Collections.Generic;

namespace TruckFlow.Domain.Dto.NotaFiscal
{
    /// <summary>
    /// Dados extraídos direto do XML da NF-e, sem nenhum enriquecimento de banco
    /// (matching de fornecedor/produto). Fonte comum tanto pro upload manual de XML
    /// quanto pra busca direta na SEFAZ (NFeDistribuicaoDFe) — ver NotaFiscalXmlExtractor.
    /// </summary>
    public class NotaFiscalXmlExtraidaDto
    {
        public required string ChaveAcesso { get; init; }
        public required long Numero { get; init; }
        public required string Serie { get; init; }
        public required DateTime DataEmissao { get; init; }
        public required string EmitenteNome { get; init; }
        public required string EmitenteCnpj { get; init; }
        public required string DestinatarioNome { get; init; }
        public required string DestinatarioCpfCnpj { get; init; }
        public required decimal ValorTotal { get; init; }
        public decimal? PesoBruto { get; init; }
        public int? VolumeQuantidade { get; init; }
        public required string PlacaVeiculo { get; init; }
        public required IReadOnlyList<NotaFiscalXmlItemExtraidoDto> Itens { get; init; }
    }
}
