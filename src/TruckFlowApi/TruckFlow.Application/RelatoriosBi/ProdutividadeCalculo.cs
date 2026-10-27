using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class ProdutividadeCalculo
    {
        public const int LimiteEsperaPadraoMin = 30;
        public const int MaximoFornecedores = 15;
        private const int MinimoCiclosDestaque = 3;

        private static readonly (string Rotulo, int De, int? Ate)[] Faixas =
        [
            ("até 5 min", 0, 5),
            ("5–10 min", 5, 10),
            ("10–15 min", 10, 15),
            ("15–30 min", 15, 30),
            ("30–45 min", 30, 45),
            ("45–60 min", 45, 60),
            ("1–1,5 h", 60, 90),
            ("1,5–2 h", 90, 120),
            ("2 h ou mais", 120, null),
        ];

        public sealed record Ciclo(RelatorioBiLinha Linha, CicloCaminhaoResultado Resultado)
        {
            public double Espera => Resultado.EsperaMinutos ?? 0;
            public double Descarga => Resultado.DescargaMinutos ?? 0;
            public double? Permanencia => Resultado.PermanenciaMinutos;
            public DateTime ChegadaLocal => RelatorioBiPeriodo.HoraLocal(Linha.ChegouEm!.Value);
        }

        public static int ResolverLimite(int? limite) =>
            limite is > 0 and <= 600 ? limite.Value : LimiteEsperaPadraoMin;

        public static IReadOnlyList<Ciclo> Ciclos(IEnumerable<RelatorioBiLinha> linhas, IReadOnlyDictionary<Guid, List<DateTime>> fins) =>
            linhas
                .Where(l => l.ChegouEm.HasValue)
                .Select(l => new Ciclo(l, CicloCaminhao.Calcular(
                    new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm),
                    fins)))
                .Where(c => c.Resultado.DescargaMinutos.HasValue && c.Resultado.EsperaMinutos.HasValue)
                .ToList();

        public static ProdutividadeResponseDto Montar(
            IReadOnlyList<RelatorioBiLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins,
            RelatorioBiPeriodoDto periodo,
            int limiteEsperaMin)
        {
            var ciclos = Ciclos(linhas, fins);
            var atual = ciclos.Where(c => RelatorioBiPeriodo.Dentro(c.Linha.DataInicio, periodo.Inicio, periodo.Fim)).ToList();
            var anterior = ciclos.Where(c => RelatorioBiPeriodo.Dentro(c.Linha.DataInicio, periodo.InicioAnterior, periodo.FimAnterior)).ToList();

            var serie = RelatorioBiPeriodo.Dias(periodo.Inicio, periodo.Fim)
                .Select(dia => Dia(dia, atual.Where(c => RelatorioBiPeriodo.DiaLocal(c.Linha.DataInicio) == dia).ToList()))
                .ToList();

            var heatmap = Heatmap(atual);
            var porDoca = Grupos(atual, c => c.Linha.LocalDescargaId, c => c.Linha.LocalDescargaNome ?? "Sem doca", limiteEsperaMin);
            var indicadores = Indicadores(atual, anterior, serie, periodo, limiteEsperaMin);

            return new ProdutividadeResponseDto
            {
                Periodo = periodo,
                LimiteEsperaMin = limiteEsperaMin,
                Indicadores = indicadores,
                SerieDiaria = serie,
                Distribuicao = Distribuicao(atual),
                Heatmap = heatmap,
                PorHora = PorHora(atual),
                PorDoca = porDoca,
                PorProduto = Grupos(atual, c => c.Linha.ProdutoId, c => c.Linha.ProdutoNome ?? "Sem produto", limiteEsperaMin),
                PorUnidade = Grupos(atual, c => c.Linha.UnidadeEntregaId, c => c.Linha.UnidadeEntregaNome ?? "Sem unidade", limiteEsperaMin),
                PorFornecedor = Grupos(atual, c => c.Linha.FornecedorId, c => c.Linha.FornecedorNome ?? "Sem fornecedor", limiteEsperaMin)
                    .Take(MaximoFornecedores)
                    .ToList(),
                Destaques = Destaques(atual, indicadores, heatmap, porDoca, limiteEsperaMin),
            };
        }

        private static ProdutividadeIndicadoresDto Indicadores(
            List<Ciclo> atual,
            List<Ciclo> anterior,
            List<ProdutividadeDiaDto> serie,
            RelatorioBiPeriodoDto periodo,
            int limite)
        {
            decimal AcimaPct(List<Ciclo> c) => c.Count == 0 ? 0 : Math.Round(c.Count(x => x.Espera > limite) * 100m / c.Count, 1);
            decimal HorasEspera(List<Ciclo> c) => Math.Round((decimal)c.Sum(x => x.Espera) / 60m, 1);

            var comDados = serie.Where(d => d.Ciclos > 0).ToList();

            return new ProdutividadeIndicadoresDto
            {
                Ciclos = RelatorioBiPeriodo.Indicador(atual.Count, anterior.Count, serie.Select(d => (decimal)d.Ciclos)),
                EsperaMediaMin = RelatorioBiPeriodo.Indicador(Media(atual, c => c.Espera) ?? 0, Media(anterior, c => c.Espera) ?? 0, comDados.Select(d => d.EsperaMediaMin!.Value)),
                EsperaP90Min = RelatorioBiPeriodo.Indicador(P(atual, c => c.Espera, 0.9) ?? 0, P(anterior, c => c.Espera, 0.9) ?? 0, comDados.Select(d => d.EsperaP90Min!.Value)),
                DescargaMediaMin = RelatorioBiPeriodo.Indicador(Media(atual, c => c.Descarga) ?? 0, Media(anterior, c => c.Descarga) ?? 0, comDados.Select(d => d.DescargaMediaMin!.Value)),
                PermanenciaMediaMin = RelatorioBiPeriodo.Indicador(
                    Media(atual.Where(c => c.Permanencia.HasValue).ToList(), c => c.Permanencia!.Value) ?? 0,
                    Media(anterior.Where(c => c.Permanencia.HasValue).ToList(), c => c.Permanencia!.Value) ?? 0,
                    comDados.Where(d => d.PermanenciaMediaMin.HasValue).Select(d => d.PermanenciaMediaMin!.Value)),
                HorasEspera = RelatorioBiPeriodo.Indicador(HorasEspera(atual), HorasEspera(anterior), comDados.Select(d => Math.Round(d.EsperaMediaMin!.Value * d.Ciclos / 60m, 1))),
                AcimaDoLimitePct = RelatorioBiPeriodo.Indicador(AcimaPct(atual), AcimaPct(anterior), []),
                EsperaMedianaMin = P(atual, c => c.Espera, 0.5),
                DescargaMedianaMin = P(atual, c => c.Descarga, 0.5),
                DescargaP90Min = P(atual, c => c.Descarga, 0.9),
                PermanenciaP90Min = P(atual.Where(c => c.Permanencia.HasValue).ToList(), c => c.Permanencia!.Value, 0.9),
                AcimaDoLimite = atual.Count(c => c.Espera > limite),
                DescargasPorDia = Math.Round((decimal)atual.Count / Math.Max(1, periodo.Dias), 1),
                EstimadosPct = atual.Count == 0 ? 0 : Math.Round(atual.Count(c => c.Resultado.InicioEstimado) * 100m / atual.Count, 1),
            };
        }

        private static ProdutividadeDiaDto Dia(DateOnly dia, List<Ciclo> ciclos) =>
            new()
            {
                Dia = dia,
                Ciclos = ciclos.Count,
                EsperaMediaMin = Media(ciclos, c => c.Espera),
                EsperaP90Min = P(ciclos, c => c.Espera, 0.9),
                DescargaMediaMin = Media(ciclos, c => c.Descarga),
                PermanenciaMediaMin = Media(ciclos.Where(c => c.Permanencia.HasValue).ToList(), c => c.Permanencia!.Value),
            };

        private static List<ProdutividadeFaixaDto> Distribuicao(List<Ciclo> ciclos)
        {
            static bool Na(double v, int de, int? ate) => v >= de && (ate is null || v < ate);

            return Faixas
                .Select(f => new ProdutividadeFaixaDto
                {
                    Rotulo = f.Rotulo,
                    DeMin = f.De,
                    AteMin = f.Ate,
                    Espera = ciclos.Count(c => Na(c.Espera, f.De, f.Ate)),
                    Descarga = ciclos.Count(c => Na(c.Descarga, f.De, f.Ate)),
                })
                .ToList();
        }

        private static List<ProdutividadeCelulaDto> Heatmap(List<Ciclo> ciclos) =>
            ciclos
                .GroupBy(c => (DiaSemana: ((int)c.ChegadaLocal.DayOfWeek + 6) % 7, c.ChegadaLocal.Hour))
                .OrderBy(g => g.Key.DiaSemana).ThenBy(g => g.Key.Hour)
                .Select(g => new ProdutividadeCelulaDto
                {
                    DiaSemana = g.Key.DiaSemana,
                    Hora = g.Key.Hour,
                    Chegadas = g.Count(),
                    EsperaMediaMin = Media(g.ToList(), c => c.Espera),
                    EsperaP90Min = P(g.ToList(), c => c.Espera, 0.9),
                })
                .ToList();

        private static List<ProdutividadeHoraDto> PorHora(List<Ciclo> ciclos) =>
            Enumerable.Range(0, 24)
                .Select(h =>
                {
                    var daHora = ciclos.Where(c => c.ChegadaLocal.Hour == h).ToList();
                    return new ProdutividadeHoraDto
                    {
                        Hora = h,
                        Chegadas = daHora.Count,
                        EsperaMediaMin = Media(daHora, c => c.Espera),
                        DescargaMediaMin = Media(daHora, c => c.Descarga),
                    };
                })
                .ToList();

        private static List<ProdutividadeGrupoDto> Grupos(
            List<Ciclo> ciclos,
            Func<Ciclo, Guid?> chave,
            Func<Ciclo, string> nome,
            int limite) =>
            ciclos
                .GroupBy(chave)
                .Select(g =>
                {
                    var lista = g.ToList();
                    return new ProdutividadeGrupoDto
                    {
                        Id = g.Key,
                        Nome = nome(lista[0]),
                        Ciclos = lista.Count,
                        EsperaMediaMin = Media(lista, c => c.Espera),
                        EsperaP90Min = P(lista, c => c.Espera, 0.9),
                        DescargaMediaMin = Media(lista, c => c.Descarga),
                        DescargaMedianaMin = P(lista, c => c.Descarga, 0.5),
                        PermanenciaMediaMin = Media(lista.Where(c => c.Permanencia.HasValue).ToList(), c => c.Permanencia!.Value),
                        AcimaDoLimite = lista.Count(c => c.Espera > limite),
                        CaixaEspera = Caixa(lista.Select(c => c.Espera).ToList()),
                        CaixaDescarga = Caixa(lista.Select(c => c.Descarga).ToList()),
                    };
                })
                .OrderByDescending(g => g.Ciclos)
                .ThenBy(g => g.Nome)
                .ToList();

        private static List<VisaoGeralDestaqueDto> Destaques(
            List<Ciclo> atual,
            ProdutividadeIndicadoresDto indicadores,
            List<ProdutividadeCelulaDto> heatmap,
            List<ProdutividadeGrupoDto> porDoca,
            int limite)
        {
            var destaques = new List<VisaoGeralDestaqueDto>();
            if (atual.Count == 0)
            {
                return destaques;
            }

            var piorJanela = heatmap
                .Where(c => c.Chegadas >= MinimoCiclosDestaque && c.EsperaMediaMin >= 10)
                .OrderByDescending(c => c.EsperaMediaMin)
                .FirstOrDefault();

            if (piorJanela is not null)
            {
                var rotulo = CapacidadeDocasCalculo.RotuloJanela(piorJanela.DiaSemana, piorJanela.Hora);
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"Quem chega {rotulo} espera {Minutos(piorJanela.EsperaMediaMin!.Value)} em média",
                    Descricao = $"Pior horário de chegada do período ({piorJanela.Chegadas} caminhões). Vale distribuir as vagas desse horário ou reforçar a doca.",
                });
            }

            if (indicadores.AcimaDoLimite > 0)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = indicadores.AcimaDoLimitePct.Valor >= 20 ? "warning" : "info",
                    Titulo = indicadores.AcimaDoLimite == 1
                        ? $"1 caminhão esperou mais de {limite} min"
                        : $"{indicadores.AcimaDoLimite} caminhões esperaram mais de {limite} min",
                    Descricao = $"{indicadores.AcimaDoLimitePct.Valor:0.#}% das descargas do período passaram do limite de espera.",
                });
            }

            var mediaDescarga = indicadores.DescargaMediaMin.Valor;
            var docaLenta = porDoca
                .Where(d => d.Ciclos >= MinimoCiclosDestaque && d.DescargaMediaMin.HasValue && mediaDescarga > 0)
                .OrderByDescending(d => d.DescargaMediaMin)
                .FirstOrDefault();

            if (docaLenta is not null && porDoca.Count(d => d.Ciclos >= MinimoCiclosDestaque) > 1)
            {
                var diferenca = Math.Round((docaLenta.DescargaMediaMin!.Value - mediaDescarga) * 100m / mediaDescarga, 0);
                if (diferenca >= 15)
                {
                    destaques.Add(new VisaoGeralDestaqueDto
                    {
                        Tipo = "warning",
                        Titulo = $"{docaLenta.Nome} descarrega {diferenca:0}% mais devagar que a média",
                        Descricao = $"{Minutos(docaLenta.DescargaMediaMin.Value)} por caminhão contra {Minutos(mediaDescarga)} na média das docas.",
                    });
                }
            }

            var variacao = indicadores.EsperaMediaMin.VariacaoPct;
            if (variacao is <= -10 or >= 10)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = variacao < 0 ? "success" : "warning",
                    Titulo = variacao < 0
                        ? $"Espera média caiu {Math.Abs(variacao.Value):0}% em relação ao período anterior"
                        : $"Espera média subiu {variacao.Value:0}% em relação ao período anterior",
                    Descricao = $"De {Minutos(indicadores.EsperaMediaMin.Anterior)} para {Minutos(indicadores.EsperaMediaMin.Valor)} por caminhão.",
                });
            }

            if (indicadores.HorasEspera.Valor > 0)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"{indicadores.HorasEspera.Valor:0.#} h de caminhão parado esperando doca",
                    Descricao = "Soma do tempo entre a chegada e o início da descarga. É o tempo que mais pesa no custo de frete.",
                });
            }

            if (indicadores.EstimadosPct > 0)
            {
                destaques.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"Início da descarga estimado em {indicadores.EstimadosPct:0.#}% dos caminhões",
                    Descricao = "Sem a captura do início real, ele é calculado pela chegada e pelo fim do caminhão anterior na mesma doca.",
                });
            }

            return destaques.Take(5).ToList();
        }

        public static ProdutividadePatioDto MontarPatio(
            IReadOnlyList<RelatorioBiLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins,
            DateOnly dia,
            DateTime agoraUtc)
        {
            var ehHoje = RelatorioBiPeriodo.DiaLocal(agoraUtc) == dia;

            var caminhoes = linhas
                .Where(l => l.ChegouEm.HasValue && RelatorioBiPeriodo.DiaLocal(l.DataInicio) == dia
                         && l.Status is StatusAgendamento.EmAndamento or StatusAgendamento.Finalizado)
                .Select(l => (Linha: l, Ciclo: CicloCaminhao.Calcular(
                    new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm),
                    fins)))
                .OrderBy(x => x.Linha.ChegouEm)
                .ToList();

            DateTime FimNoPatio((RelatorioBiLinha Linha, CicloCaminhaoResultado Ciclo) x) =>
                x.Ciclo.Saida ?? (ehHoje ? Maior(agoraUtc, x.Linha.ChegouEm!.Value) : Maior(x.Linha.DataFim, x.Linha.ChegouEm!.Value));

            var (pico, picoEm) = Pico(caminhoes.Select(x => (x.Linha.ChegouEm!.Value, FimNoPatio(x))).ToList());

            var docas = caminhoes
                .GroupBy(x => x.Linha.LocalDescargaId)
                .Select(g => new ProdutividadePatioDocaDto
                {
                    Id = g.Key,
                    Nome = g.First().Linha.LocalDescargaNome ?? "Sem doca",
                    Unidade = g.First().Linha.UnidadeEntregaNome,
                    DescargaMediaMin = Media(g.Where(x => x.Ciclo.DescargaMinutos.HasValue).Select(x => x.Ciclo.DescargaMinutos!.Value).ToList()),
                    Caminhoes = g.Select(x => new ProdutividadePatioCaminhaoDto
                    {
                        AgendamentoId = x.Linha.Id,
                        Placa = x.Linha.PlacaVeiculo,
                        Motorista = x.Linha.MotoristaNome,
                        Fornecedor = x.Linha.FornecedorNome,
                        Produto = x.Linha.ProdutoNome,
                        Status = x.Linha.Status.ToString(),
                        JanelaInicio = x.Linha.DataInicio,
                        JanelaFim = x.Linha.DataFim,
                        ChegouEm = x.Linha.ChegouEm!.Value,
                        InicioDescarga = x.Ciclo.InicioDescarga,
                        InicioEstimado = x.Ciclo.InicioEstimado,
                        FimDescarga = x.Linha.FimDescargaEm,
                        Saida = x.Ciclo.Saida,
                        SaidaEstimada = x.Ciclo.SaidaEstimada,
                        EsperaMin = Arredondar(x.Ciclo.EsperaMinutos),
                        DescargaMin = Arredondar(x.Ciclo.DescargaMinutos),
                    }).ToList(),
                })
                .OrderBy(d => d.Nome)
                .ToList();

            var inicios = caminhoes.SelectMany(x => new[] { x.Linha.ChegouEm!.Value, x.Linha.DataInicio }).ToList();
            var fimEixo = caminhoes.Select(FimNoPatio).Concat(caminhoes.Select(x => x.Linha.DataFim)).DefaultIfEmpty().Max();

            return new ProdutividadePatioDto
            {
                Dia = dia,
                AgoraUtc = ehHoje ? agoraUtc : null,
                InicioEixo = caminhoes.Count == 0 ? null : HoraCheia(inicios.Min(), arredondarPraCima: false),
                FimEixo = caminhoes.Count == 0 ? null : HoraCheia(fimEixo, arredondarPraCima: true),
                Caminhoes = caminhoes.Count,
                EsperaMediaMin = Media(caminhoes.Where(x => x.Ciclo.EsperaMinutos.HasValue).Select(x => x.Ciclo.EsperaMinutos!.Value).ToList()),
                DescargaMediaMin = Media(caminhoes.Where(x => x.Ciclo.DescargaMinutos.HasValue).Select(x => x.Ciclo.DescargaMinutos!.Value).ToList()),
                PicoSimultaneos = pico,
                PicoEm = picoEm,
                Docas = docas,
            };
        }

        public static (int Pico, DateTime? Em) Pico(IReadOnlyList<(DateTime Inicio, DateTime Fim)> intervalos)
        {
            var eventos = intervalos
                .SelectMany(i => new[] { (Quando: i.Inicio, Delta: 1), (Quando: i.Fim, Delta: -1) })
                .OrderBy(e => e.Quando)
                .ThenBy(e => e.Delta)
                .ToList();

            var atual = 0;
            var pico = 0;
            DateTime? em = null;
            foreach (var e in eventos)
            {
                atual += e.Delta;
                if (atual > pico)
                {
                    pico = atual;
                    em = e.Quando;
                }
            }

            return (pico, em);
        }

        public static ProdutividadeCaixaDto? Caixa(IReadOnlyCollection<double> valores)
        {
            if (valores.Count == 0)
            {
                return null;
            }

            return new ProdutividadeCaixaDto
            {
                Min = (decimal)Math.Round(valores.Min(), 1),
                Q1 = (decimal)RelatorioDetalhadoCalculo.Percentil(valores, 0.25)!.Value,
                Mediana = (decimal)RelatorioDetalhadoCalculo.Percentil(valores, 0.5)!.Value,
                Q3 = (decimal)RelatorioDetalhadoCalculo.Percentil(valores, 0.75)!.Value,
                Max = (decimal)Math.Round(valores.Max(), 1),
            };
        }

        private static decimal? Media(IReadOnlyCollection<Ciclo> ciclos, Func<Ciclo, double> valor) =>
            Media(ciclos.Select(valor).ToList());

        private static decimal? Media(IReadOnlyCollection<double> valores) =>
            valores.Count == 0 ? null : (decimal)Math.Round(valores.Average(), 1);

        private static decimal? P(IReadOnlyCollection<Ciclo> ciclos, Func<Ciclo, double> valor, double p) =>
            ciclos.Count == 0 ? null : (decimal?)RelatorioDetalhadoCalculo.Percentil(ciclos.Select(valor).ToList(), p);

        private static decimal? Arredondar(double? valor) => valor.HasValue ? (decimal)Math.Round(valor.Value, 1) : null;

        private static DateTime Maior(DateTime a, DateTime b) => a > b ? a : b;

        private static DateTime HoraCheia(DateTime utc, bool arredondarPraCima)
        {
            var cheia = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc);
            return arredondarPraCima && cheia < utc ? cheia.AddHours(1) : cheia;
        }

        private static string Minutos(decimal minutos)
        {
            var total = (int)Math.Round(minutos);
            if (total < 60)
            {
                return $"{total} min";
            }

            var resto = total % 60;
            return resto == 0 ? $"{total / 60} h" : $"{total / 60} h {resto:00} min";
        }
    }
}
