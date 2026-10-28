using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;
using TruckFlow.Domain.Dto.Relatorio;

namespace TruckFlow.Application.Relatorios
{
    public static class RelatorioXlsxWriter
    {
        private const string FormatoDataHora = "dd/MM/yyyy HH:mm";

        public sealed class Linha
        {
            [ExcelColumn(Name = "Janela início", Width = 17, Format = FormatoDataHora)] public DateTime JanelaInicio { get; init; }
            [ExcelColumn(Name = "Janela fim", Width = 17, Format = FormatoDataHora)] public DateTime JanelaFim { get; init; }
            [ExcelColumn(Name = "Status", Width = 16)] public string Status { get; init; } = "";
            [ExcelColumn(Name = "Origem", Width = 10)] public string Origem { get; init; } = "";
            [ExcelColumn(Name = "Motorista", Width = 28)] public string? Motorista { get; init; }
            [ExcelColumn(Name = "Telefone", Width = 16)] public string? Telefone { get; init; }
            [ExcelColumn(Name = "Placa", Width = 10)] public string? Placa { get; init; }
            [ExcelColumn(Name = "Tipo de veículo", Width = 18)] public string? TipoVeiculo { get; init; }
            [ExcelColumn(Name = "Fornecedor", Width = 30)] public string? Fornecedor { get; init; }
            [ExcelColumn(Name = "Produto", Width = 22)] public string Produto { get; init; } = "";
            [ExcelColumn(Name = "Nota fiscal", Width = 12)] public long? NotaFiscal { get; init; }
            [ExcelColumn(Name = "Unidade", Width = 20)] public string? Unidade { get; init; }
            [ExcelColumn(Name = "Doca", Width = 14)] public string? Doca { get; init; }
            [ExcelColumn(Name = "Peso (kg)", Width = 12, Format = "#,##0")] public decimal? PesoKg { get; init; }
            [ExcelColumn(Name = "Chegada", Width = 17, Format = FormatoDataHora)] public DateTime? Chegada { get; init; }
            [ExcelColumn(Name = "Situação da chegada", Width = 18)] public string? SituacaoChegada { get; init; }
            [ExcelColumn(Name = "Início da descarga", Width = 17, Format = FormatoDataHora)] public DateTime? InicioDescarga { get; init; }
            [ExcelColumn(Name = "Início estimado", Width = 10)] public string? InicioEstimado { get; init; }
            [ExcelColumn(Name = "Fim da descarga", Width = 17, Format = FormatoDataHora)] public DateTime? FimDescarga { get; init; }
            [ExcelColumn(Name = "Saída", Width = 17, Format = FormatoDataHora)] public DateTime? Saida { get; init; }
            [ExcelColumn(Name = "Espera (min)", Width = 11, Format = "0")] public double? EsperaMin { get; init; }
            [ExcelColumn(Name = "Descarga (min)", Width = 11, Format = "0")] public double? DescargaMin { get; init; }
            [ExcelColumn(Name = "Permanência (min)", Width = 11, Format = "0")] public double? PermanenciaMin { get; init; }
        }

        public static Linha Converter(AgendamentoRelatorioLinha l)
        {
            var ciclo = l.Ciclo;

            return new Linha
            {
                JanelaInicio = RelatorioFormatacao.ParaHorarioLocal(l.DataInicio),
                JanelaFim = RelatorioFormatacao.ParaHorarioLocal(l.DataFim),
                Status = RelatorioDetalhadoCalculo.StatusRotulo(l.Status),
                Origem = l.DaGrade ? "Grade" : "Avulso",
                Motorista = l.MotoristaNome,
                Telefone = l.MotoristaTelefone,
                Placa = l.PlacaVeiculo,
                TipoVeiculo = RelatorioDetalhadoCalculo.TipoVeiculoRotulo(l.TipoVeiculo),
                Fornecedor = l.FornecedorNome,
                Produto = l.Produto,
                NotaFiscal = l.NotaFiscalNumero,
                Unidade = l.UnidadeEntrega,
                Doca = l.LocalDescarga,
                PesoKg = l.PesoCarga,
                Chegada = Local(l.ChegouEm),
                SituacaoChegada = RelatorioDetalhadoCalculo.SituacaoRotulo(
                    RelatorioDetalhadoCalculo.Situacao(l.Status, l.DataInicio, l.DataFim, l.ChegouEm)),
                InicioDescarga = Local(ciclo?.InicioDescarga),
                InicioEstimado = ciclo?.InicioDescarga is null ? null : ciclo.InicioEstimado ? "Sim" : "Não",
                FimDescarga = Local(l.FimDescargaEm),
                Saida = Local(ciclo?.Saida),
                EsperaMin = ciclo?.EsperaMinutos,
                DescargaMin = ciclo?.DescargaMinutos,
                PermanenciaMin = ciclo?.PermanenciaMinutos
            };
        }

        public static Task EscreverAsync(IEnumerable<AgendamentoRelatorioLinha> linhas, Stream destino, CancellationToken token = default)
        {
            var configuracao = new OpenXmlConfiguration
            {
                AutoFilter = true,
                FreezeRowCount = 1,
                TableStyles = TableStyles.Default
            };

            return destino.SaveAsAsync(
                linhas.Select(Converter),
                sheetName: "Agendamentos",
                excelType: ExcelType.XLSX,
                configuration: configuracao,
                cancellationToken: token);
        }

        private static DateTime? Local(DateTime? utc) =>
            utc.HasValue ? RelatorioFormatacao.ParaHorarioLocal(utc.Value) : null;
    }
}
