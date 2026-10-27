using System.Globalization;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class VisaoGeralCalculo
    {
        private const int TopRanking = 5;
        private static readonly CultureInfo PtBr = new("pt-BR");
        private static readonly string[] DiasSemana = ["segundas", "terças", "quartas", "quintas", "sextas", "sábados", "domingos"];

        public static VisaoGeralResponseDto Montar(
            IReadOnlyCollection<RelatorioBiLinha> linhas,
            RelatorioBiPeriodoDto periodo,
            decimal? metaDiariaKg,
            int docasAtivas)
        {
            var atual = linhas.Where(l => RelatorioBiPeriodo.Dentro(l.DataInicio, periodo.Inicio, periodo.Fim)).ToList();
            var anterior = linhas.Where(l => RelatorioBiPeriodo.Dentro(l.DataInicio, periodo.InicioAnterior, periodo.FimAnterior)).ToList();

            var serie = SerieDiaria(atual, periodo.Inicio, periodo.Fim);
            var serieAnterior = SerieDiaria(anterior, periodo.InicioAnterior, periodo.FimAnterior);

            var indicadores = Indicadores(atual, anterior, serie, metaDiariaKg, periodo.Dias);
            var heatmap = Heatmap(atual);
            var topFornecedores = Ranking(atual, l => l.FornecedorId, l => l.FornecedorNome ?? "Sem fornecedor");
            var topProdutos = Ranking(atual, l => l.ProdutoId, l => l.ProdutoNome ?? "Sem produto");

            return new VisaoGeralResponseDto
            {
                Periodo = periodo,
                Indicadores = indicadores,
                SerieDiaria = serie,
                SerieDiariaAnterior = serieAnterior,
                MetaDiariaKg = metaDiariaKg,
                PorStatus = new VisaoGeralStatusDto
                {
                    Agendado = atual.Count(l => l.Status == StatusAgendamento.Agendado),
                    EmAndamento = atual.Count(l => l.Status == StatusAgendamento.EmAndamento),
                    Finalizado = atual.Count(l => l.Status == StatusAgendamento.Finalizado),
                    Cancelado = atual.Count(l => l.Status == StatusAgendamento.Cancelado),
                    Expirado = atual.Count(l => l.Status == StatusAgendamento.Expirado),
                },
                Funil = new VisaoGeralFunilDto
                {
                    Reservados = atual.Count(l => l.Status != StatusAgendamento.Cancelado),
                    Chegaram = atual.Count(Chegou),
                    Concluidos = atual.Count(l => l.Status == StatusAgendamento.Finalizado),
                },
                HeatmapDiaHora = heatmap,
                OcupacaoPorHora = OcupacaoPorHora(atual, docasAtivas, periodo.Dias),
                DocasAtivas = docasAtivas,
                TopFornecedores = topFornecedores,
                TopProdutos = topProdutos,
                Destaques = Destaques(indicadores, heatmap, topFornecedores),
            };
        }

        private static bool Chegou(RelatorioBiLinha l) =>
            l.ChegouEm.HasValue
            || l.Status == StatusAgendamento.EmAndamento
            || l.Status == StatusAgendamento.Finalizado;

        private static bool Compareceu(RelatorioBiLinha l) =>
            l.Status is StatusAgendamento.EmAndamento or StatusAgendamento.Finalizado;

        private static decimal Peso(IEnumerable<RelatorioBiLinha> linhas) =>
            linhas.Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg);

        private static decimal? Comparecimento(IReadOnlyCollection<RelatorioBiLinha> linhas)
        {
            var compareceu = linhas.Count(Compareceu);
            var faltou = linhas.Count(l => l.Status == StatusAgendamento.Expirado);
            return compareceu + faltou == 0 ? null : Math.Round(compareceu * 100m / (compareceu + faltou), 1);
        }

        private static List<VisaoGeralDiaDto> SerieDiaria(List<RelatorioBiLinha> linhas, DateOnly inicio, DateOnly fim)
        {
            var porDia = linhas.GroupBy(l => RelatorioBiPeriodo.DiaLocal(l.DataInicio)).ToDictionary(g => g.Key, g => g.ToList());

            return RelatorioBiPeriodo.Dias(inicio, fim)
                .Select(dia =>
                {
                    var doDia = porDia.TryGetValue(dia, out var lista) ? lista : [];
                    return new VisaoGeralDiaDto
                    {
                        Data = dia,
                        Agendamentos = doDia.Count,
                        Concluidos = doDia.Count(l => l.Status == StatusAgendamento.Finalizado),
                        Cancelados = doDia.Count(l => l.Status == StatusAgendamento.Cancelado),
                        NaoCompareceu = doDia.Count(l => l.Status == StatusAgendamento.Expirado),
                        PesoKg = Peso(doDia),
                    };
                })
                .ToList();
        }

        private static VisaoGeralIndicadoresDto Indicadores(
            List<RelatorioBiLinha> atual,
            List<RelatorioBiLinha> anterior,
            List<VisaoGeralDiaDto> serie,
            decimal? metaDiariaKg,
            int dias)
        {
            var concluidosAtual = atual.Count(l => l.Status == StatusAgendamento.Finalizado);
            var concluidosAnterior = anterior.Count(l => l.Status == StatusAgendamento.Finalizado);
            var pesoAtual = Peso(atual);
            var pesoAnterior = Peso(anterior);

            var porDia = atual.GroupBy(l => RelatorioBiPeriodo.DiaLocal(l.DataInicio)).ToDictionary(g => g.Key, g => (IReadOnlyCollection<RelatorioBiLinha>)g.ToList());

            return new VisaoGeralIndicadoresDto
            {
                Agendamentos = RelatorioBiPeriodo.Indicador(atual.Count, anterior.Count, serie.Select(d => (decimal)d.Agendamentos)),
                Concluidos = RelatorioBiPeriodo.Indicador(concluidosAtual, concluidosAnterior, serie.Select(d => (decimal)d.Concluidos)),
                PesoRecebidoKg = RelatorioBiPeriodo.Indicador(pesoAtual, pesoAnterior, serie.Select(d => d.PesoKg)),
                ComparecimentoPct = RelatorioBiPeriodo.Indicador(
                    Comparecimento(atual) ?? 0,
                    Comparecimento(anterior) ?? 0,
                    serie.Select(d => porDia.TryGetValue(d.Data, out var doDia) ? Comparecimento(doDia) ?? 0 : 0)),
                Cancelados = RelatorioBiPeriodo.Indicador(
                    atual.Count(l => l.Status == StatusAgendamento.Cancelado),
                    anterior.Count(l => l.Status == StatusAgendamento.Cancelado),
                    serie.Select(d => (decimal)d.Cancelados)),
                NaoCompareceu = RelatorioBiPeriodo.Indicador(
                    atual.Count(l => l.Status == StatusAgendamento.Expirado),
                    anterior.Count(l => l.Status == StatusAgendamento.Expirado),
                    serie.Select(d => (decimal)d.NaoCompareceu)),
                PesoMedioKg = RelatorioBiPeriodo.Indicador(
                    concluidosAtual == 0 ? 0 : Math.Round(pesoAtual / concluidosAtual, 0),
                    concluidosAnterior == 0 ? 0 : Math.Round(pesoAnterior / concluidosAnterior, 0),
                    serie.Select(d => d.Concluidos == 0 ? 0 : Math.Round(d.PesoKg / d.Concluidos, 0))),
                AtingimentoMetaPct = metaDiariaKg is > 0
                    ? Math.Round(pesoAtual * 100m / (metaDiariaKg.Value * dias), 1)
                    : null,
            };
        }

        private static List<List<int>> Heatmap(List<RelatorioBiLinha> linhas)
        {
            var matriz = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();

            foreach (var linha in linhas.Where(l => l.Status != StatusAgendamento.Cancelado))
            {
                var hora = RelatorioBiPeriodo.HoraLocal(linha.DataInicio);
                var dia = ((int)hora.DayOfWeek + 6) % 7;
                matriz[dia][hora.Hour]++;
            }

            return matriz.Select(d => d.ToList()).ToList();
        }

        private static List<VisaoGeralOcupacaoHoraDto> OcupacaoPorHora(List<RelatorioBiLinha> linhas, int docasAtivas, int dias)
        {
            var minutos = new decimal[24];
            var inicios = new int[24];

            foreach (var linha in linhas.Where(l => l.Status != StatusAgendamento.Cancelado))
            {
                var inicio = RelatorioBiPeriodo.HoraLocal(linha.DataInicio);
                var fim = RelatorioBiPeriodo.HoraLocal(linha.DataFim);
                inicios[inicio.Hour]++;

                var cursor = inicio;
                while (cursor < fim)
                {
                    var proximaHora = cursor.Date.AddHours(cursor.Hour + 1);
                    var limite = proximaHora < fim ? proximaHora : fim;
                    minutos[cursor.Hour] += (decimal)(limite - cursor).TotalMinutes;
                    cursor = limite;
                }
            }

            var capacidade = docasAtivas * 60m * dias;

            return Enumerable.Range(0, 24)
                .Select(h => new VisaoGeralOcupacaoHoraDto
                {
                    Hora = h,
                    OcupacaoPct = capacidade == 0 ? 0 : Math.Min(100, Math.Round(minutos[h] * 100m / capacidade, 1)),
                    Agendamentos = dias == 0 ? 0 : Math.Round((decimal)inicios[h] / dias, 1),
                })
                .ToList();
        }

        private static List<RelatorioBiRankingDto> Ranking(
            List<RelatorioBiLinha> linhas,
            Func<RelatorioBiLinha, Guid?> id,
            Func<RelatorioBiLinha, string> nome)
        {
            var pesoTotal = Peso(linhas);

            return linhas
                .GroupBy(l => (Id: id(l), Nome: nome(l)))
                .Select(g => new RelatorioBiRankingDto
                {
                    Id = g.Key.Id,
                    Nome = g.Key.Nome,
                    Agendamentos = g.Count(),
                    Concluidos = g.Count(l => l.Status == StatusAgendamento.Finalizado),
                    PesoKg = Peso(g),
                })
                .OrderByDescending(r => r.PesoKg)
                .ThenByDescending(r => r.Agendamentos)
                .ThenBy(r => r.Nome)
                .Take(TopRanking)
                .Select(r =>
                {
                    r.Percentual = pesoTotal == 0 ? 0 : Math.Round(r.PesoKg * 100m / pesoTotal, 1);
                    return r;
                })
                .ToList();
        }

        private static List<VisaoGeralDestaqueDto> Destaques(
            VisaoGeralIndicadoresDto indicadores,
            List<List<int>> heatmap,
            List<RelatorioBiRankingDto> topFornecedores)
        {
            var destaques = new List<VisaoGeralDestaqueDto>();

            var pico = heatmap
                .SelectMany((horas, dia) => horas.Select((total, hora) => (dia, hora, total)))
                .OrderByDescending(c => c.total)
                .FirstOrDefault();

            if (pico.total > 0)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"Pico nas {DiasSemana[pico.dia]} às {pico.hora}h",
                    Descricao = $"{pico.total} agendamentos começaram nesse horário no período.",
                });
            }

            var variacaoPeso = indicadores.PesoRecebidoKg.VariacaoPct;
            if (variacaoPeso.HasValue)
            {
                var subiu = variacaoPeso.Value >= 0;
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = subiu ? "success" : "warning",
                    Titulo = $"Volume recebido {(subiu ? "+" : "")}{variacaoPeso.Value.ToString("0.#", PtBr)}%",
                    Descricao = $"{Toneladas(indicadores.PesoRecebidoKg.Valor)} contra {Toneladas(indicadores.PesoRecebidoKg.Anterior)} no período anterior.",
                });
            }

            if (indicadores.AtingimentoMetaPct.HasValue)
            {
                var atingimento = indicadores.AtingimentoMetaPct.Value;
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = atingimento >= 100 ? "success" : atingimento >= 80 ? "info" : "warning",
                    Titulo = atingimento >= 100 ? "Meta do período atingida" : $"{atingimento.ToString("0.#", PtBr)}% da meta do período",
                    Descricao = "Meta diária da empresa multiplicada pelos dias do período.",
                });
            }

            var comparecimento = indicadores.ComparecimentoPct.Valor;
            if (indicadores.NaoCompareceu.Valor > 0 && comparecimento < 90)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"Comparecimento de {comparecimento.ToString("0.#", PtBr)}%",
                    Descricao = $"{indicadores.NaoCompareceu.Valor:0} caminhões agendados não vieram no período.",
                });
            }

            var lider = topFornecedores.FirstOrDefault();
            if (lider is { Percentual: >= 30m } && topFornecedores.Count > 1)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"{lider.Nome} concentrou {lider.Percentual.ToString("0.#", PtBr)}% do volume",
                    Descricao = $"{lider.Concluidos} descargas concluídas, {Toneladas(lider.PesoKg)}.",
                });
            }

            return destaques.Take(4).ToList();
        }

        private static string Toneladas(decimal kg) => $"{(kg / 1000m).ToString("#,##0.0", PtBr)} t";
    }
}
