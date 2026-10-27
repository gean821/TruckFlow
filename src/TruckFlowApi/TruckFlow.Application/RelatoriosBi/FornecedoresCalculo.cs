using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class FornecedoresCalculo
    {
        public const int MinimoParaScore = 3;
        public const int MaximoOcorrencias = 20;
        private const string SemFornecedor = "Sem fornecedor";
        public const string SemCadastro = "Sem cadastro";
        private static readonly string[] DiasSemana = ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"];

        private static readonly (string Rotulo, int? De, int? Ate, bool Atrasado)[] FaixasChegada =
        [
            ("Mais de 1 h antes", null, -60, false),
            ("30 min a 1 h antes", -60, -30, false),
            ("Até 30 min antes", -30, 0, false),
            ("Dentro da janela", 0, 0, false),
            ("Até 30 min depois", 0, 30, true),
            ("30 min a 1 h depois", 30, 60, true),
            ("Mais de 1 h depois", 60, null, true),
        ];

        public sealed record Medidas(
            int Agendamentos,
            int Realizaveis,
            int Concluidos,
            decimal PesoKg,
            int Chegaram,
            int Adiantados,
            int NoHorario,
            int Atrasados,
            decimal? AtrasoMedioMin,
            int NaoCompareceu,
            int Cancelados)
        {
            public decimal? PontualidadePct => Pct(Adiantados + NoHorario, Chegaram);
            public decimal? NoShowPct => Pct(NaoCompareceu, Realizaveis);
            public decimal? CancelamentoPct => Pct(Cancelados, Agendamentos);

            public decimal? Score =>
                Realizaveis < MinimoParaScore
                    ? null
                    : Math.Round(0.5m * (PontualidadePct ?? 0) + 0.3m * (100 - (NoShowPct ?? 0)) + 0.2m * (100 - (CancelamentoPct ?? 0)), 0);
        }

        public static Medidas Medir(IReadOnlyCollection<RelatorioBiLinha> linhas)
        {
            var situacoes = linhas
                .Select(l => (Linha: l, Situacao: RelatorioDetalhadoCalculo.Situacao(l.Status, l.DataInicio, l.DataFim, l.ChegouEm)))
                .ToList();

            var atrasados = situacoes.Where(s => s.Situacao == SituacaoChegada.Atrasado).ToList();

            return new Medidas(
                linhas.Count,
                linhas.Count(l => l.Status is StatusAgendamento.Finalizado or StatusAgendamento.EmAndamento or StatusAgendamento.Expirado),
                linhas.Count(l => l.Status == StatusAgendamento.Finalizado),
                linhas.Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg),
                linhas.Count(l => l.ChegouEm.HasValue),
                situacoes.Count(s => s.Situacao == SituacaoChegada.Adiantado),
                situacoes.Count(s => s.Situacao == SituacaoChegada.NoHorario),
                atrasados.Count,
                atrasados.Count == 0
                    ? null
                    : Math.Round((decimal)atrasados.Average(s => (s.Linha.ChegouEm!.Value - s.Linha.DataInicio).TotalMinutes), 1),
                situacoes.Count(s => s.Situacao == SituacaoChegada.NaoCompareceu),
                linhas.Count(l => l.Status == StatusAgendamento.Cancelado));
        }

        public static string Classificar(decimal? score) => score switch
        {
            null => "Poucos dados",
            >= 85 => "Excelente",
            >= 70 => "Bom",
            >= 50 => "Atenção",
            _ => "Crítico",
        };

        public static FornecedoresResponseDto Montar(
            IReadOnlyList<RelatorioBiLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins,
            RelatorioBiPeriodoDto periodo,
            string agrupamento)
        {
            var (atual, anterior) = Separar(linhas, periodo);
            var ciclos = Ciclos(linhas, fins);
            var baldes = RecebimentosCalculo.Baldes(periodo, agrupamento);
            var ranking = Ranking(atual, anterior, ciclos, baldes);

            var serie = baldes.Select(b => Balde(b, atual)).ToList();
            var medidasAtual = Medir(atual);
            var medidasAnterior = Medir(anterior);
            var comScore = ranking.Where(r => r.Score.HasValue).ToList();
            var comFornecedor = ranking.Where(r => r.Id.HasValue).ToList();

            var indicadores = new FornecedoresIndicadoresDto
            {
                FornecedoresAtivos = RelatorioBiPeriodo.Indicador(
                    Ativos(atual),
                    Ativos(anterior),
                    baldes.Select(b => (decimal)Ativos(Do(atual, b)))),
                PontualidadePct = RelatorioBiPeriodo.Indicador(medidasAtual.PontualidadePct ?? 0, medidasAnterior.PontualidadePct ?? 0, serie.Where(s => s.PontualidadePct.HasValue).Select(s => s.PontualidadePct!.Value)),
                NoShowPct = RelatorioBiPeriodo.Indicador(medidasAtual.NoShowPct ?? 0, medidasAnterior.NoShowPct ?? 0, serie.Where(s => s.NoShowPct.HasValue).Select(s => s.NoShowPct!.Value)),
                CancelamentoPct = RelatorioBiPeriodo.Indicador(medidasAtual.CancelamentoPct ?? 0, medidasAnterior.CancelamentoPct ?? 0, serie.Where(s => s.CancelamentoPct.HasValue).Select(s => s.CancelamentoPct!.Value)),
                AtrasoMedioMin = RelatorioBiPeriodo.Indicador(medidasAtual.AtrasoMedioMin ?? 0, medidasAnterior.AtrasoMedioMin ?? 0, []),
                PesoRecebidoKg = RelatorioBiPeriodo.Indicador(medidasAtual.PesoKg, medidasAnterior.PesoKg, serie.Select(s => s.PesoKg)),
                ScoreMedio = comScore.Count == 0 ? null : Math.Round(comScore.Average(r => r.Score!.Value), 0),
                ConcentracaoTop3Pct = comFornecedor.Count >= 4 ? comFornecedor.OrderByDescending(r => r.PesoKg).Take(3).Sum(r => r.PctVolume) : null,
                ComScoreCritico = comScore.Count(r => r.Score < 50),
            };

            return new FornecedoresResponseDto
            {
                Periodo = periodo,
                Agrupamento = agrupamento,
                Indicadores = indicadores,
                Serie = serie,
                Ranking = ranking,
                Destaques = Destaques(ranking, indicadores),
            };
        }

        public static FornecedorDetalheDto Detalhe(
            IReadOnlyList<RelatorioBiLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins,
            RelatorioBiPeriodoDto periodo,
            string agrupamento,
            Guid fornecedorId)
        {
            var (atual, anterior) = Separar(linhas, periodo);
            var ciclos = Ciclos(linhas, fins);
            var baldes = RecebimentosCalculo.Baldes(periodo, agrupamento);
            var ranking = Ranking(atual, anterior, ciclos, baldes);
            var pesoTotal = atual.Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg);

            var doFornecedor = atual.Where(l => l.FornecedorId == fornecedorId).ToList();
            var anteriorDoFornecedor = anterior.Where(l => l.FornecedorId == fornecedorId).ToList();
            var nome = doFornecedor.Concat(anteriorDoFornecedor).Select(l => l.FornecedorNome).FirstOrDefault(n => n is not null) ?? "Fornecedor";
            var posicao = ranking.FindIndex(r => r.Id == fornecedorId);

            return new FornecedorDetalheDto
            {
                Periodo = periodo,
                Agrupamento = agrupamento,
                Fornecedor = Desempenho(fornecedorId, nome, doFornecedor, anteriorDoFornecedor, pesoTotal, ciclos, baldes),
                Media = Desempenho(null, "Todos os fornecedores", atual, anterior, pesoTotal, ciclos, baldes),
                Posicao = posicao + 1,
                TotalFornecedores = ranking.Count(r => r.Id.HasValue),
                Serie = baldes.Select(b => Balde(b, doFornecedor)).ToList(),
                PorProduto = Grupos(doFornecedor, l => l.ProdutoNome ?? "Sem produto"),
                PorUnidade = Grupos(doFornecedor, l => l.UnidadeEntregaNome ?? "Sem unidade"),
                PorDiaSemana = Enumerable.Range(0, 7)
                    .Select(d =>
                    {
                        var doDia = doFornecedor.Where(l => DiaSemana(l.DataInicio) == d).ToList();
                        var m = Medir(doDia);
                        return new FornecedorGrupoDto
                        {
                            Nome = DiasSemana[d],
                            Agendamentos = m.Agendamentos,
                            PesoKg = m.PesoKg,
                            PontualidadePct = m.PontualidadePct,
                            NoShowPct = m.NoShowPct,
                            CancelamentoPct = m.CancelamentoPct,
                        };
                    })
                    .ToList(),
                DistribuicaoChegada = DistribuicaoChegada(doFornecedor),
                Ocorrencias = Ocorrencias(doFornecedor),
            };
        }

        public static List<FornecedorFaixaChegadaDto> DistribuicaoChegada(IEnumerable<RelatorioBiLinha> linhas)
        {
            var desvios = linhas
                .Where(l => l.ChegouEm.HasValue)
                .Select(l => Desvio(l.ChegouEm!.Value, l.DataInicio, l.DataFim))
                .ToList();

            return FaixasChegada
                .Select(f => new FornecedorFaixaChegadaDto
                {
                    Rotulo = f.Rotulo,
                    DeMin = f.De,
                    AteMin = f.Ate,
                    Atrasado = f.Atrasado,
                    Quantidade = desvios.Count(d => NaFaixa(d, f)),
                })
                .ToList();
        }

        private static (double Minutos, bool Dentro, bool Depois) Desvio(DateTime chegou, DateTime inicio, DateTime fim)
        {
            if (chegou < inicio) return ((chegou - inicio).TotalMinutes, false, false);
            if (chegou <= fim) return (0, true, false);
            return ((chegou - fim).TotalMinutes, false, true);
        }

        private static bool NaFaixa((double Minutos, bool Dentro, bool Depois) d, (string Rotulo, int? De, int? Ate, bool Atrasado) f)
        {
            if (f.Rotulo == "Dentro da janela") return d.Dentro;
            if (d.Dentro || d.Depois != f.Atrasado) return false;
            return (f.De is null || d.Minutos >= f.De) && (f.Ate is null || d.Minutos < f.Ate);
        }

        private static List<FornecedorOcorrenciaDto> Ocorrencias(IEnumerable<RelatorioBiLinha> linhas) =>
            linhas
                .Select(l => (Linha: l, Situacao: RelatorioDetalhadoCalculo.Situacao(l.Status, l.DataInicio, l.DataFim, l.ChegouEm)))
                .Where(x => x.Situacao is SituacaoChegada.Atrasado or SituacaoChegada.NaoCompareceu || x.Linha.Status == StatusAgendamento.Cancelado)
                .OrderByDescending(x => x.Linha.DataInicio)
                .Take(MaximoOcorrencias)
                .Select(x => new FornecedorOcorrenciaDto
                {
                    AgendamentoId = x.Linha.Id,
                    DataInicio = x.Linha.DataInicio,
                    DataFim = x.Linha.DataFim,
                    Tipo = x.Linha.Status == StatusAgendamento.Cancelado
                        ? "Cancelado"
                        : x.Situacao == SituacaoChegada.NaoCompareceu ? "Não compareceu" : "Atraso",
                    AtrasoMin = x.Situacao == SituacaoChegada.Atrasado
                        ? Math.Round((decimal)(x.Linha.ChegouEm!.Value - x.Linha.DataInicio).TotalMinutes, 0)
                        : null,
                    Placa = x.Linha.PlacaVeiculo,
                    Motorista = x.Linha.MotoristaNome,
                    Produto = x.Linha.ProdutoNome,
                    Doca = x.Linha.LocalDescargaNome,
                })
                .ToList();

        private static List<FornecedorDesempenhoDto> Ranking(
            List<RelatorioBiLinha> atual,
            List<RelatorioBiLinha> anterior,
            IReadOnlyDictionary<Guid, CicloCaminhaoResultado> ciclos,
            List<(DateOnly Inicio, DateOnly Fim)> baldes)
        {
            var pesoTotal = atual.Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg);

            return atual
                .GroupBy(l => l.FornecedorId)
                .Select(g => Desempenho(
                    g.Key,
                    g.Select(l => l.FornecedorNome).FirstOrDefault(n => n is not null) ?? SemFornecedor,
                    g.ToList(),
                    anterior.Where(l => l.FornecedorId == g.Key).ToList(),
                    pesoTotal,
                    ciclos,
                    baldes))
                .Select(SemCadastroNaoTemScore)
                .OrderByDescending(r => r.PesoKg)
                .ThenByDescending(r => r.Agendamentos)
                .ThenBy(r => r.Nome)
                .ToList();
        }

        private static FornecedorDesempenhoDto SemCadastroNaoTemScore(FornecedorDesempenhoDto f)
        {
            if (f.Id.HasValue)
                return f;

            f.Score = null;
            f.Classificacao = SemCadastro;
            return f;
        }

        private static FornecedorDesempenhoDto Desempenho(
            Guid? id,
            string nome,
            List<RelatorioBiLinha> atual,
            List<RelatorioBiLinha> anterior,
            decimal pesoTotal,
            IReadOnlyDictionary<Guid, CicloCaminhaoResultado> ciclos,
            List<(DateOnly Inicio, DateOnly Fim)> baldes)
        {
            var m = Medir(atual);
            var a = Medir(anterior);
            var doCiclo = atual.Where(l => ciclos.ContainsKey(l.Id)).Select(l => ciclos[l.Id]).ToList();

            return new FornecedorDesempenhoDto
            {
                Id = id,
                Nome = nome,
                Agendamentos = m.Agendamentos,
                Realizaveis = m.Realizaveis,
                Concluidos = m.Concluidos,
                PesoKg = m.PesoKg,
                PctVolume = pesoTotal == 0 ? 0 : Math.Round(m.PesoKg * 100m / pesoTotal, 1),
                Chegaram = m.Chegaram,
                Adiantados = m.Adiantados,
                NoHorario = m.NoHorario,
                Atrasados = m.Atrasados,
                PontualidadePct = m.PontualidadePct,
                PontualidadeAnteriorPct = a.PontualidadePct,
                VariacaoPontualidade = m.PontualidadePct.HasValue && a.PontualidadePct.HasValue
                    ? Math.Round(m.PontualidadePct.Value - a.PontualidadePct.Value, 1)
                    : null,
                AtrasoMedioMin = m.AtrasoMedioMin,
                NaoCompareceu = m.NaoCompareceu,
                NoShowPct = m.NoShowPct,
                Cancelados = m.Cancelados,
                CancelamentoPct = m.CancelamentoPct,
                EsperaMediaMin = Media(doCiclo.Where(c => c.EsperaMinutos.HasValue).Select(c => c.EsperaMinutos!.Value)),
                DescargaMediaMin = Media(doCiclo.Where(c => c.DescargaMinutos.HasValue).Select(c => c.DescargaMinutos!.Value)),
                Score = m.Score,
                Classificacao = Classificar(m.Score),
                ProdutoPrincipal = atual
                    .Where(l => l.ProdutoNome is not null)
                    .GroupBy(l => l.ProdutoNome)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault(),
                SeriePontualidade = baldes.Select(b => Medir(Do(atual, b)).PontualidadePct).ToList(),
            };
        }

        private static List<VisaoGeralDestaqueDto> Destaques(List<FornecedorDesempenhoDto> ranking, FornecedoresIndicadoresDto indicadores)
        {
            var destaques = new List<VisaoGeralDestaqueDto>();
            var avaliados = ranking.Where(r => r.Id.HasValue && r.Score.HasValue).ToList();

            var melhor = avaliados.OrderByDescending(r => r.Score).ThenByDescending(r => r.PesoKg).FirstOrDefault();
            if (melhor is not null && avaliados.Count > 1)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "success",
                    Titulo = $"{melhor.Nome} é o fornecedor mais confiável (score {melhor.Score:0})",
                    Descricao = $"{Pct(melhor.PontualidadePct)} de pontualidade e {Pct(melhor.NoShowPct)} de faltas em {melhor.Agendamentos} agendamentos.",
                });
            }

            var faltoso = avaliados.Where(r => r.NoShowPct >= 10).OrderByDescending(r => r.NoShowPct).FirstOrDefault();
            if (faltoso is not null)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"{faltoso.Nome} faltou em {Pct(faltoso.NoShowPct)} dos agendamentos",
                    Descricao = $"{faltoso.NaoCompareceu} vaga(s) reservada(s) ficaram vazias. Vale conversar com o fornecedor ou liberar a vaga antes.",
                });
            }

            var atrasado = ranking
                .Where(r => r.Id.HasValue && r.Chegaram >= MinimoParaScore && r.PontualidadePct < 80)
                .OrderBy(r => r.PontualidadePct)
                .FirstOrDefault();
            if (atrasado is not null)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"{atrasado.Nome} chegou depois da janela em {Pct(100 - atrasado.PontualidadePct)} das vezes",
                    Descricao = atrasado.AtrasoMedioMin.HasValue
                        ? $"Em média {Minutos(atrasado.AtrasoMedioMin.Value)} depois do início da janela."
                        : "Atrasos frequentes empurram a fila da doca.",
                });
            }

            var piorou = ranking
                .Where(r => r.Id.HasValue && r.VariacaoPontualidade <= -10)
                .OrderBy(r => r.VariacaoPontualidade)
                .FirstOrDefault();
            if (piorou is not null)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"Pontualidade de {piorou.Nome} caiu {Math.Abs(piorou.VariacaoPontualidade!.Value):0} pontos",
                    Descricao = $"De {Pct(piorou.PontualidadeAnteriorPct)} no período anterior para {Pct(piorou.PontualidadePct)}.",
                });
            }

            if (indicadores.ConcentracaoTop3Pct >= 60)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"3 fornecedores concentram {indicadores.ConcentracaoTop3Pct:0}% do volume",
                    Descricao = "Dependência alta: um problema em qualquer um deles afeta boa parte do recebimento.",
                });
            }

            var cancelador = avaliados.Where(r => r.CancelamentoPct >= 15).OrderByDescending(r => r.CancelamentoPct).FirstOrDefault();
            if (cancelador is not null)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"{cancelador.Nome} cancelou {Pct(cancelador.CancelamentoPct)} dos agendamentos",
                    Descricao = $"{cancelador.Cancelados} cancelamento(s) no período.",
                });
            }

            return destaques.Take(5).ToList();
        }

        private static FornecedoresBaldeDto Balde((DateOnly Inicio, DateOnly Fim) balde, List<RelatorioBiLinha> linhas)
        {
            var m = Medir(Do(linhas, balde));
            return new FornecedoresBaldeDto
            {
                Inicio = balde.Inicio,
                Fim = balde.Fim,
                Rotulo = RecebimentosCalculo.RotuloBalde(balde),
                Agendamentos = m.Agendamentos,
                PesoKg = m.PesoKg,
                PontualidadePct = m.PontualidadePct,
                NoShowPct = m.NoShowPct,
                CancelamentoPct = m.CancelamentoPct,
            };
        }

        private static List<FornecedorGrupoDto> Grupos(List<RelatorioBiLinha> linhas, Func<RelatorioBiLinha, string> chave) =>
            linhas
                .GroupBy(chave)
                .Select(g =>
                {
                    var m = Medir(g.ToList());
                    return new FornecedorGrupoDto
                    {
                        Nome = g.Key,
                        Agendamentos = m.Agendamentos,
                        PesoKg = m.PesoKg,
                        PontualidadePct = m.PontualidadePct,
                        NoShowPct = m.NoShowPct,
                        CancelamentoPct = m.CancelamentoPct,
                    };
                })
                .OrderByDescending(g => g.PesoKg)
                .ThenByDescending(g => g.Agendamentos)
                .ToList();

        private static (List<RelatorioBiLinha> Atual, List<RelatorioBiLinha> Anterior) Separar(IReadOnlyList<RelatorioBiLinha> linhas, RelatorioBiPeriodoDto periodo) =>
            (linhas.Where(l => RelatorioBiPeriodo.Dentro(l.DataInicio, periodo.Inicio, periodo.Fim)).ToList(),
             linhas.Where(l => RelatorioBiPeriodo.Dentro(l.DataInicio, periodo.InicioAnterior, periodo.FimAnterior)).ToList());

        private static Dictionary<Guid, CicloCaminhaoResultado> Ciclos(IEnumerable<RelatorioBiLinha> linhas, IReadOnlyDictionary<Guid, List<DateTime>> fins) =>
            linhas
                .Where(l => l.ChegouEm.HasValue)
                .ToDictionary(
                    l => l.Id,
                    l => CicloCaminhao.Calcular(new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm), fins));

        private static List<RelatorioBiLinha> Do(IEnumerable<RelatorioBiLinha> linhas, (DateOnly Inicio, DateOnly Fim) balde) =>
            linhas.Where(l => RelatorioBiPeriodo.Dentro(l.DataInicio, balde.Inicio, balde.Fim)).ToList();

        private static int Ativos(IEnumerable<RelatorioBiLinha> linhas) =>
            linhas.Where(l => l.FornecedorId.HasValue).Select(l => l.FornecedorId).Distinct().Count();

        private static int DiaSemana(DateTime utc) => ((int)RelatorioBiPeriodo.HoraLocal(utc).DayOfWeek + 6) % 7;

        private static decimal? Pct(int parte, int total) => total == 0 ? null : Math.Round(parte * 100m / total, 1);

        private static string Pct(decimal? valor) => valor.HasValue ? $"{valor.Value:0.#}%" : "—";

        private static decimal? Media(IEnumerable<double> valores)
        {
            var lista = valores.ToList();
            return lista.Count == 0 ? null : (decimal)Math.Round(lista.Average(), 1);
        }

        private static string Minutos(decimal minutos)
        {
            var total = (int)Math.Round(minutos);
            if (total < 60) return $"{total} min";
            var resto = total % 60;
            return resto == 0 ? $"{total / 60} h" : $"{total / 60} h {resto:00} min";
        }
    }
}
