using System.Globalization;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class RecebimentosCalculo
    {
        public const int TopProdutosSerie = 5;
        public const int TopRanking = 10;
        public const int TopFluxoFornecedores = 6;
        public const int TopDivergencias = 10;
        public const string Outros = "Outros";

        private static readonly CultureInfo PtBr = new("pt-BR");
        private static readonly string[] AgrupamentosValidos = ["dia", "semana", "mes"];

        public static string ResolverAgrupamento(string? agrupamento, int dias)
        {
            if (string.IsNullOrWhiteSpace(agrupamento))
                return dias <= 45 ? "dia" : dias <= 200 ? "semana" : "mes";

            var normalizado = agrupamento.Trim().ToLowerInvariant();
            if (!AgrupamentosValidos.Contains(normalizado))
                throw new BusinessException("Agrupamento inválido. Use dia, semana ou mês.");

            return normalizado;
        }

        public static RecebimentosResponseDto Montar(
            IReadOnlyCollection<RecebimentoLinha> recebimentos,
            IReadOnlyCollection<PlanejamentoItemLinha> planejamento,
            (int Quantidade, decimal Kg) orfaos,
            RelatorioBiPeriodoDto periodo,
            string agrupamento,
            decimal? metaDiariaKg,
            DateOnly hoje)
        {
            var atual = recebimentos.Where(r => RelatorioBiPeriodo.Dentro(r.DataInicio, periodo.Inicio, periodo.Fim)).ToList();
            var anterior = recebimentos.Where(r => RelatorioBiPeriodo.Dentro(r.DataInicio, periodo.InicioAnterior, periodo.FimAnterior)).ToList();

            var produtosSerie = atual
                .GroupBy(Produto)
                .OrderByDescending(g => g.Sum(r => r.PesoNotaKg))
                .Select(g => g.Key)
                .Take(TopProdutosSerie)
                .ToList();

            var baldes = Baldes(periodo, agrupamento);
            var serie = baldes.Select(b => Balde(b, atual, produtosSerie, metaDiariaKg)).ToList();
            if (serie.Any(b => b.PorProduto.ContainsKey(Outros)))
                produtosSerie.Add(Outros);

            var serieDiariaAtual = RelatorioBiPeriodo.Dias(periodo.Inicio, periodo.Fim)
                .Select(dia => atual.Where(r => RelatorioBiPeriodo.DiaLocal(r.DataInicio) == dia).Sum(r => r.PesoNotaKg))
                .ToList();

            var planejamentoDto = Planejamento(planejamento, periodo, hoje);

            return new RecebimentosResponseDto
            {
                Periodo = periodo,
                Agrupamento = agrupamento,
                Indicadores = Indicadores(atual, anterior, serieDiariaAtual, planejamentoDto, orfaos, metaDiariaKg, periodo.Dias),
                Produtos = produtosSerie,
                Serie = serie,
                PorProduto = Ranking(atual, Produto, r => r.ProdutoId),
                PorFornecedor = Ranking(atual, Fornecedor, r => r.FornecedorId),
                PorUnidade = Ranking(atual, r => r.UnidadeEntregaNome ?? "Sem unidade", r => r.UnidadeEntregaId),
                Fluxo = Fluxo(atual),
                Planejamento = planejamentoDto,
                Divergencias = Divergencias(atual),
                Matriz = Matriz(atual),
            };
        }

        private static string Produto(RecebimentoLinha r) => r.ProdutoNome ?? "Sem produto";

        private static string Fornecedor(RecebimentoLinha r) => r.FornecedorNome ?? "Sem fornecedor";

        private static RecebimentosIndicadoresDto Indicadores(
            List<RecebimentoLinha> atual,
            List<RecebimentoLinha> anterior,
            List<decimal> serieDiaria,
            List<RecebimentosPlanejamentoDto> planejamento,
            (int Quantidade, decimal Kg) orfaos,
            decimal? metaDiariaKg,
            int dias)
        {
            var pesoAtual = atual.Sum(r => r.PesoNotaKg);
            var pesoAnterior = anterior.Sum(r => r.PesoNotaKg);
            var conferidos = atual.Where(r => r.PesoRecebidoKg.HasValue).ToList();
            var notaConferida = conferidos.Sum(r => r.PesoNotaKg);
            var recebido = conferidos.Sum(r => r.PesoRecebidoKg!.Value);
            var planejado = planejamento.Sum(p => p.PlanejadoKg);

            return new RecebimentosIndicadoresDto
            {
                PesoNotaKg = RelatorioBiPeriodo.Indicador(pesoAtual, pesoAnterior, serieDiaria),
                Descargas = RelatorioBiPeriodo.Indicador(atual.Count, anterior.Count, []),
                PesoMedioKg = RelatorioBiPeriodo.Indicador(
                    atual.Count == 0 ? 0 : Math.Round(pesoAtual / atual.Count, 0),
                    anterior.Count == 0 ? 0 : Math.Round(pesoAnterior / anterior.Count, 0),
                    []),
                PesoRecebidoKg = conferidos.Count == 0 ? null : recebido,
                PesoNotaConferidoKg = conferidos.Count == 0 ? null : notaConferida,
                DivergenciaKg = conferidos.Count == 0 ? null : recebido - notaConferida,
                DivergenciaPct = conferidos.Count == 0 || notaConferida == 0 ? null : Math.Round((recebido - notaConferida) * 100m / notaConferida, 2),
                CoberturaConferenciaPct = atual.Count == 0 ? 0 : Math.Round(conferidos.Count * 100m / atual.Count, 1),
                CumprimentoPlanejamentoPct = planejado == 0 ? null : Math.Round(planejamento.Sum(p => p.RecebidoKg) * 100m / planejado, 1),
                Orfaos = orfaos.Quantidade,
                OrfaosKg = orfaos.Kg,
                MetaPeriodoKg = metaDiariaKg is > 0 ? metaDiariaKg.Value * dias : null,
            };
        }

        private static List<(DateOnly Inicio, DateOnly Fim)> Baldes(RelatorioBiPeriodoDto periodo, string agrupamento)
        {
            var baldes = new List<(DateOnly, DateOnly)>();
            var cursor = periodo.Inicio;

            while (cursor <= periodo.Fim)
            {
                var fim = agrupamento switch
                {
                    "semana" => cursor.AddDays(6 - (((int)cursor.DayOfWeek + 6) % 7)),
                    "mes" => new DateOnly(cursor.Year, cursor.Month, 1).AddMonths(1).AddDays(-1),
                    _ => cursor,
                };

                if (fim > periodo.Fim)
                    fim = periodo.Fim;

                baldes.Add((cursor, fim));
                cursor = fim.AddDays(1);
            }

            return baldes;
        }

        private static RecebimentosBaldeDto Balde(
            (DateOnly Inicio, DateOnly Fim) balde,
            List<RecebimentoLinha> atual,
            List<string> produtosSerie,
            decimal? metaDiariaKg)
        {
            var doBalde = atual.Where(r => RelatorioBiPeriodo.Dentro(r.DataInicio, balde.Inicio, balde.Fim)).ToList();
            var dias = balde.Fim.DayNumber - balde.Inicio.DayNumber + 1;

            var porProduto = doBalde
                .GroupBy(r => produtosSerie.Contains(Produto(r)) ? Produto(r) : Outros)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.PesoNotaKg));

            return new RecebimentosBaldeDto
            {
                Inicio = balde.Inicio,
                Fim = balde.Fim,
                Rotulo = balde.Inicio == balde.Fim
                    ? balde.Inicio.ToString("dd/MM", PtBr)
                    : balde.Inicio.Day == 1 && balde.Fim.AddDays(1).Day == 1
                        ? balde.Inicio.ToString("MMM/yy", PtBr).Replace(".", "")
                        : $"{balde.Inicio:dd/MM}–{balde.Fim:dd/MM}",
                TotalKg = doBalde.Sum(r => r.PesoNotaKg),
                Descargas = doBalde.Count,
                PorProduto = porProduto,
                MetaKg = metaDiariaKg is > 0 ? metaDiariaKg.Value * dias : null,
            };
        }

        private static List<RelatorioBiRankingDto> Ranking(
            List<RecebimentoLinha> linhas,
            Func<RecebimentoLinha, string> nome,
            Func<RecebimentoLinha, Guid?> id)
        {
            var total = linhas.Sum(r => r.PesoNotaKg);

            return linhas
                .GroupBy(r => (Nome: nome(r), Id: id(r)))
                .Select(g => new RelatorioBiRankingDto
                {
                    Id = g.Key.Id,
                    Nome = g.Key.Nome,
                    Agendamentos = g.Count(),
                    Concluidos = g.Count(),
                    PesoKg = g.Sum(r => r.PesoNotaKg),
                    Percentual = total == 0 ? 0 : Math.Round(g.Sum(r => r.PesoNotaKg) * 100m / total, 1),
                })
                .OrderByDescending(r => r.PesoKg)
                .ThenBy(r => r.Nome)
                .Take(TopRanking)
                .ToList();
        }

        private static RecebimentosFluxoDto Fluxo(List<RecebimentoLinha> linhas)
        {
            const string outrosFornecedores = "Outros fornecedores";
            const string outrosProdutos = "Outros produtos";

            var fornecedoresTop = linhas.GroupBy(Fornecedor)
                .OrderByDescending(g => g.Sum(r => r.PesoNotaKg))
                .Take(TopFluxoFornecedores)
                .Select(g => g.Key)
                .ToHashSet();

            var produtosTop = linhas.GroupBy(Produto)
                .OrderByDescending(g => g.Sum(r => r.PesoNotaKg))
                .Take(TopProdutosSerie)
                .Select(g => g.Key)
                .ToHashSet();

            string NomeFornecedor(RecebimentoLinha r) => fornecedoresTop.Contains(Fornecedor(r)) ? Fornecedor(r) : outrosFornecedores;
            string NomeProduto(RecebimentoLinha r) => produtosTop.Contains(Produto(r)) ? Produto(r) : outrosProdutos;
            string NomeUnidade(RecebimentoLinha r) => r.UnidadeEntregaNome ?? "Sem unidade";

            var ligacoes = linhas.GroupBy(r => (Origem: $"f:{NomeFornecedor(r)}", Destino: $"p:{NomeProduto(r)}"))
                .Concat(linhas.GroupBy(r => (Origem: $"p:{NomeProduto(r)}", Destino: $"u:{NomeUnidade(r)}")))
                .Select(g => new RecebimentosFluxoLigacaoDto { Origem = g.Key.Origem, Destino = g.Key.Destino, Kg = g.Sum(r => r.PesoNotaKg) })
                .Where(l => l.Kg > 0)
                .OrderByDescending(l => l.Kg)
                .ToList();

            var nos = linhas.Select(r => (Id: $"f:{NomeFornecedor(r)}", Nome: NomeFornecedor(r), Tipo: "fornecedor"))
                .Concat(linhas.Select(r => (Id: $"p:{NomeProduto(r)}", Nome: NomeProduto(r), Tipo: "produto")))
                .Concat(linhas.Select(r => (Id: $"u:{NomeUnidade(r)}", Nome: NomeUnidade(r), Tipo: "unidade")))
                .Distinct()
                .Select(n => new RecebimentosFluxoNoDto { Id = n.Id, Nome = n.Nome, Tipo = n.Tipo })
                .ToList();

            return new RecebimentosFluxoDto { Nos = nos, Ligacoes = ligacoes };
        }

        private static List<RecebimentosPlanejamentoDto> Planejamento(
            IReadOnlyCollection<PlanejamentoItemLinha> itens,
            RelatorioBiPeriodoDto periodo,
            DateOnly hoje)
        {
            var limite = periodo.Fim < hoje ? periodo.Fim : hoje;

            return itens
                .Select(item =>
                {
                    var inicio = Maior(periodo.Inicio, RelatorioBiPeriodo.DiaLocal(item.PlanejamentoInicio));
                    var fim = Menor(limite, RelatorioBiPeriodo.DiaLocal(item.PlanejamentoFim));
                    var diasOperacao = DiasDeOperacao(item.DiasSemana);

                    var diasNoPeriodo = inicio > fim
                        ? 0
                        : RelatorioBiPeriodo.Dias(inicio, fim).Count(d => diasOperacao.Count == 0 || diasOperacao.Contains(d.DayOfWeek));

                    var planejado = Math.Min(item.CadenciaDiariaKg * diasNoPeriodo, item.QuantidadeTotalPlanejadaKg);
                    decimal? cumprimento = planejado == 0 ? null : Math.Round(item.RecebidoNoPeriodoKg * 100m / planejado, 1);

                    return new RecebimentosPlanejamentoDto
                    {
                        ItemId = item.ItemId,
                        Produto = item.ProdutoNome,
                        Fornecedor = item.FornecedorNome,
                        PlanejadoKg = planejado,
                        RecebidoKg = item.RecebidoNoPeriodoKg,
                        CumprimentoPct = cumprimento,
                        Situacao = cumprimento switch
                        {
                            null => "Sem previsão no período",
                            >= 105 => "Adiantado",
                            >= 90 => "No ritmo",
                            _ => "Atrasado",
                        },
                    };
                })
                .OrderBy(p => p.CumprimentoPct ?? decimal.MaxValue)
                .ThenBy(p => p.Fornecedor)
                .ToList();
        }

        private static HashSet<DayOfWeek> DiasDeOperacao(string diasSemana) =>
            string.IsNullOrWhiteSpace(diasSemana)
                ? []
                : diasSemana.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => int.TryParse(d, out var n) ? (DayOfWeek?)n : null)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .ToHashSet();

        private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;

        private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;

        private static List<RecebimentosDivergenciaDto> Divergencias(List<RecebimentoLinha> linhas) =>
            linhas
                .Where(r => r.PesoRecebidoKg.HasValue && r.PesoRecebidoKg.Value != r.PesoNotaKg)
                .Select(r => new RecebimentosDivergenciaDto
                {
                    AgendamentoId = r.AgendamentoId,
                    DataInicio = r.DataInicio,
                    Placa = r.PlacaVeiculo,
                    NotaNumero = r.NotaNumero,
                    Fornecedor = r.FornecedorNome,
                    Produto = r.ProdutoNome,
                    PesoNotaKg = r.PesoNotaKg,
                    PesoRecebidoKg = r.PesoRecebidoKg!.Value,
                    DiferencaKg = r.PesoRecebidoKg.Value - r.PesoNotaKg,
                    DiferencaPct = r.PesoNotaKg == 0 ? null : Math.Round((r.PesoRecebidoKg.Value - r.PesoNotaKg) * 100m / r.PesoNotaKg, 2),
                })
                .OrderByDescending(d => Math.Abs(d.DiferencaKg))
                .Take(TopDivergencias)
                .ToList();

        private static RecebimentosMatrizDto Matriz(List<RecebimentoLinha> linhas)
        {
            var produtos = linhas.GroupBy(Produto)
                .OrderByDescending(g => g.Sum(r => r.PesoNotaKg))
                .Select(g => g.Key)
                .ToList();

            var produtosTop = produtos.Take(TopProdutosSerie).ToList();
            if (produtos.Count > TopProdutosSerie)
                produtosTop.Add(Outros);

            string Coluna(RecebimentoLinha r) => produtosTop.Contains(Produto(r)) ? Produto(r) : Outros;

            var porFornecedor = linhas.GroupBy(Fornecedor)
                .OrderByDescending(g => g.Sum(r => r.PesoNotaKg))
                .ToList();

            var linhasMatriz = porFornecedor.Take(TopRanking)
                .Select(g => new RecebimentosMatrizLinhaDto
                {
                    Fornecedor = g.Key,
                    Valores = g.GroupBy(Coluna).ToDictionary(c => c.Key, c => c.Sum(r => r.PesoNotaKg)),
                    Total = g.Sum(r => r.PesoNotaKg),
                    Descargas = g.Count(),
                })
                .ToList();

            var resto = porFornecedor.Skip(TopRanking).SelectMany(g => g).ToList();
            if (resto.Count > 0)
            {
                linhasMatriz.Add(new RecebimentosMatrizLinhaDto
                {
                    Fornecedor = "Outros fornecedores",
                    Valores = resto.GroupBy(Coluna).ToDictionary(c => c.Key, c => c.Sum(r => r.PesoNotaKg)),
                    Total = resto.Sum(r => r.PesoNotaKg),
                    Descargas = resto.Count,
                });
            }

            return new RecebimentosMatrizDto
            {
                Produtos = produtosTop,
                Linhas = linhasMatriz,
                TotalPorProduto = linhas.GroupBy(Coluna).ToDictionary(c => c.Key, c => c.Sum(r => r.PesoNotaKg)),
                TotalGeral = linhas.Sum(r => r.PesoNotaKg),
            };
        }
    }
}
