using System.Text;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QuestPDF.Infrastructure;
using TruckFlow.Application;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;
using static TruckFlow.Test.Relatorios.RelatorioTestData;

namespace TruckFlow.Test.Relatorios
{
    public class RelatorioServiceTests
    {
        private readonly IAgendamentoRepositorio _repo = Substitute.For<IAgendamentoRepositorio>();

        // 19/10/2026 13:30 UTC = 10:30 em São Paulo
        private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 10, 19, 13, 30, 0, TimeSpan.Zero));

        private readonly RelatorioService _service;

        static RelatorioServiceTests()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public RelatorioServiceTests()
        {
            _service = new RelatorioService(_repo, _relogio);
            _repo.ListarFinsDescargaAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
                .Returns(new List<FimDescargaDoca>());
        }

        private static RelatorioAgendamentoFilterDto FiltroValido() => new()
        {
            DataInicio = new DateTime(2026, 10, 1),
            DataFim = new DateTime(2026, 10, 31)
        };

        private void BancoRetornaTotal(int total) =>
            _repo.CountLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<CancellationToken>())
                .Returns(total);

        private static async Task<byte[]> Executar(RelatorioArquivoDto arquivo)
        {
            using var destino = new MemoryStream();
            await arquivo.EscreverConteudo(destino, CancellationToken.None);
            return destino.ToArray();
        }

        // ============= Validação =============

        [Fact]
        public async Task Filtro_invalido_nao_consulta_o_banco()
        {
            var filtro = new RelatorioAgendamentoFilterDto();

            await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GerarRelatorioAgendamentos(filtro, FormatoRelatorio.Csv));

            await _repo.DidNotReceiveWithAnyArgs().CountLinhasRelatorioAsync(default!, default);
        }

        [Fact]
        public async Task Formato_inexistente_lanca_BusinessException()
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GerarRelatorioAgendamentos(FiltroValido(), (FormatoRelatorio)99));

            Assert.Contains("Formato", ex.Message);
        }

        [Fact]
        public async Task Repassa_para_o_repositorio_o_periodo_ja_convertido_para_UTC()
        {
            BancoRetornaTotal(0);

            await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv);

            await _repo.Received(1).CountLinhasRelatorioAsync(
                Arg.Is<RelatorioAgendamentoCriteria>(c =>
                    c.DataInicioUtc == new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc)),
                Arg.Any<CancellationToken>());
        }

        // ============= Limites =============

        [Fact]
        public async Task Csv_acima_do_limite_lanca_e_nao_le_as_linhas()
        {
            BancoRetornaTotal(RelatorioLimites.MaxLinhasCsv + 1);

            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv));

            Assert.Contains("100.001", ex.Message);
            Assert.Contains("100.000", ex.Message);
            _repo.DidNotReceiveWithAnyArgs().StreamLinhasRelatorioAsync(default!, default);
        }

        [Fact]
        public async Task Csv_exatamente_no_limite_e_permitido()
        {
            BancoRetornaTotal(RelatorioLimites.MaxLinhasCsv);

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv);

            Assert.NotNull(arquivo);
        }

        [Fact]
        public async Task Pdf_acima_do_limite_sugere_exportar_em_CSV()
        {
            BancoRetornaTotal(RelatorioLimites.MaxLinhasPdf + 1);

            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Pdf));

            Assert.Contains("5.000", ex.Message);
            Assert.Contains("exporte em CSV", ex.Message);
            await _repo.DidNotReceiveWithAnyArgs().GetLinhasRelatorioAsync(default!, default, default);
        }

        // ============= CSV =============

        [Fact]
        public async Task Csv_usa_nome_com_horario_de_Sao_Paulo_e_content_type_utf8()
        {
            BancoRetornaTotal(1);

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv);

            Assert.Equal("relatorio-agendamentos-20261019-1030.csv", arquivo.NomeArquivo);
            Assert.Equal("text/csv; charset=utf-8", arquivo.ContentType);
        }

        [Fact]
        public async Task Csv_so_le_o_banco_quando_o_conteudo_e_escrito()
        {
            BancoRetornaTotal(2);
            _repo.StreamLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>())
                .Returns(_ => new[] { Linha(), Linha(fornecedor: "Fornecedor B") }.ToAsyncEnumerable());

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv);

            // Até aqui nenhuma linha foi lida: o service só devolveu "como escrever"
            _repo.DidNotReceiveWithAnyArgs().StreamLinhasRelatorioAsync(default!, default);

            var csv = LerTexto(await Executar(arquivo));

            _repo.Received(1).StreamLinhasRelatorioAsync(
                Arg.Any<RelatorioAgendamentoCriteria>(), RelatorioLimites.MaxLinhasCsv);
            Assert.StartsWith(RelatorioCsvWriter.Cabecalho, csv);
            Assert.Contains("Fornecedor B", csv);
        }

        [Fact]
        public async Task Csv_nao_usa_a_consulta_em_lista()
        {
            BancoRetornaTotal(1);
            _repo.StreamLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>())
                .Returns(_ => new[] { Linha() }.ToAsyncEnumerable());

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Csv);
            await Executar(arquivo);

            await _repo.DidNotReceiveWithAnyArgs().GetLinhasRelatorioAsync(default!, default, default);
        }

        // ============= PDF =============

        [Fact]
        public async Task Pdf_gera_arquivo_valido_com_limite_do_pdf()
        {
            BancoRetornaTotal(2);
            _repo.GetLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<AgendamentoRelatorioLinha> { Linha(), Linha(peso: null) });

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Pdf);
            var bytes = await Executar(arquivo);

            Assert.Equal("relatorio-agendamentos-20261019-1030.pdf", arquivo.NomeArquivo);
            Assert.Equal("application/pdf", arquivo.ContentType);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            await _repo.Received(1).GetLinhasRelatorioAsync(
                Arg.Any<RelatorioAgendamentoCriteria>(), RelatorioLimites.MaxLinhasPdf, Arg.Any<CancellationToken>());
            _repo.DidNotReceiveWithAnyArgs().StreamLinhasRelatorioAsync(default!, default);
        }

        [Fact]
        public async Task Pdf_sem_registros_ainda_gera_documento()
        {
            BancoRetornaTotal(0);
            _repo.GetLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<AgendamentoRelatorioLinha>());

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Pdf);
            var bytes = await Executar(arquivo);

            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }

        private static readonly Guid Doca = Guid.NewGuid();

        private static AgendamentoRelatorioLinha ComDescarga(DateTime chegou, DateTime fim) => new()
        {
            Id = Guid.NewGuid(),
            LocalDescargaId = Doca,
            DaGrade = true,
            TipoCarga = TipoCarga.Milho,
            PlacaVeiculo = "ABC1D23",
            Status = StatusAgendamento.Finalizado,
            DataInicio = chegou,
            DataFim = chegou.AddHours(1),
            ChegouEm = chegou,
            FimDescargaEm = fim
        };

        [Fact]
        public async Task Xlsx_gera_planilha_com_o_ciclo_estimado_pelo_caminhao_anterior()
        {
            var chegou = LocalParaUtc(2026, 10, 1, 8, 0);
            BancoRetornaTotal(1);
            _repo.GetLinhasRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<AgendamentoRelatorioLinha> { ComDescarga(chegou, chegou.AddMinutes(60)) });
            _repo.ListarFinsDescargaAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
                .Returns(new List<FimDescargaDoca> { new(Doca, chegou.AddMinutes(25)) });

            var arquivo = await _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Xlsx);
            var bytes = await Executar(arquivo);

            Assert.Equal("relatorio-agendamentos-20261019-1030.xlsx", arquivo.NomeArquivo);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", arquivo.ContentType);
            await _repo.Received(1).GetLinhasRelatorioAsync(
                Arg.Any<RelatorioAgendamentoCriteria>(), RelatorioLimites.MaxLinhasXlsx, Arg.Any<CancellationToken>());

            var linhas = MiniExcelLibs.MiniExcel.Query(new MemoryStream(bytes), useHeaderRow: true)
                .Cast<IDictionary<string, object>>()
                .ToList();
            var linha = Assert.Single(linhas);
            Assert.Equal("Concluído", linha["Status"]);
            Assert.Equal("ABC1D23", linha["Placa"]);
            Assert.Equal("Sim", linha["Início estimado"]);
            Assert.Equal(25d, Convert.ToDouble(linha["Espera (min)"]));
            Assert.Equal(35d, Convert.ToDouble(linha["Descarga (min)"]));
        }

        [Fact]
        public async Task Xlsx_acima_do_limite_lanca_e_nao_le_as_linhas()
        {
            BancoRetornaTotal(RelatorioLimites.MaxLinhasXlsx + 1);

            await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GerarRelatorioAgendamentos(FiltroValido(), FormatoRelatorio.Xlsx));

            await _repo.DidNotReceiveWithAnyArgs().GetLinhasRelatorioAsync(default!, default, default);
        }

        [Fact]
        public async Task Detalhado_limita_a_pagina_e_estima_o_inicio_com_o_fim_anterior_na_doca()
        {
            var chegou = LocalParaUtc(2026, 10, 1, 8, 0);
            var linha = ComDescarga(chegou, chegou.AddMinutes(60));
            _repo.GetPaginaRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new PagedResponse<AgendamentoRelatorioLinha> { Items = [linha], PageNumber = 1, PageSize = 100, TotalCount = 1, TotalPages = 1 });
            _repo.ListarFinsDescargaAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
                .Returns(new List<FimDescargaDoca> { new(Doca, chegou.AddMinutes(30)), new(Doca, chegou.AddMinutes(60)) });

            var query = new RelatorioAgendamentoDetalhadoQueryDto
            {
                DataInicio = new DateTime(2026, 10, 1),
                DataFim = new DateTime(2026, 10, 1),
                PageNumber = 0,
                PageSize = 1000
            };

            var pagina = await _service.ListarDetalhadoAsync(query);

            await _repo.Received(1).GetPaginaRelatorioAsync(Arg.Any<RelatorioAgendamentoCriteria>(), 1, RelatorioLimites.MaxPorPagina, Arg.Any<CancellationToken>());
            await _repo.Received(1).ListarFinsDescargaAsync(
                chegou, chegou.AddMinutes(60),
                Arg.Is<IReadOnlyCollection<Guid>?>(d => d != null && d.Single() == Doca),
                Arg.Any<CancellationToken>());

            var dto = Assert.Single(pagina.Items);
            Assert.True(dto.InicioEstimado);
            Assert.Equal(chegou.AddMinutes(30), dto.InicioDescarga);
            Assert.Equal(30d, dto.EsperaMin);
            Assert.Equal(30d, dto.DescargaMin);
            Assert.Equal("Grade", dto.Origem);
            Assert.Equal("NoHorario", dto.SituacaoChegada);
        }

        [Fact]
        public async Task Detalhado_com_ordenacao_invalida_nao_consulta_o_banco()
        {
            var query = new RelatorioAgendamentoDetalhadoQueryDto
            {
                DataInicio = new DateTime(2026, 10, 1),
                DataFim = new DateTime(2026, 10, 1),
                OrdenarPor = "senha"
            };

            await Assert.ThrowsAsync<BusinessException>(() => _service.ListarDetalhadoAsync(query));

            await _repo.DidNotReceiveWithAnyArgs().GetPaginaRelatorioAsync(default!, default, default, default);
        }
    }
}
