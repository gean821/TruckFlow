using System.Globalization;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class CapacidadeDocasCalculo
    {
        public const decimal LimiteEsgotadoPct = 90m;
        public const decimal LimiteOciosoPct = 40m;
        private const int TopJanelas = 8;
        private static readonly CultureInfo PtBr = new("pt-BR");
        private static readonly string[] DiasSemana = ["segundas", "terças", "quartas", "quintas", "sextas", "sábados", "domingos"];

        public enum Classe
        {
            ForaDaOferta,
            Livre,
            Reservada,
            Realizada,
            NaoCompareceu,
        }

        private sealed record Vaga(
            CapacidadeVagaLinha Linha,
            Classe Classe,
            DateTime Inicio,
            DateTime Fim,
            DateTime? InicioReal,
            DateTime? FimReal,
            double? DescargaMinutos)
        {
            public DateOnly Dia => DateOnly.FromDateTime(Inicio);
            public int DiaSemana => ((int)Inicio.DayOfWeek + 6) % 7;
            public bool Ofertada => Classe != Classe.ForaDaOferta && Linha.DaGrade;
            public bool Reservada => Classe is Classe.Reservada or Classe.Realizada or Classe.NaoCompareceu;
            public bool ReservaNaGrade => Ofertada && Reservada;
            public bool ForaDaGrade => !Linha.DaGrade && Reservada;
            public decimal MinutosVaga => (decimal)(Fim - Inicio).TotalMinutes;
        }

        private sealed class Acumulado
        {
            public decimal Ofertado;
            public decimal Reservado;
            public decimal Utilizado;
            public decimal NaoCompareceu;

            public decimal? TaxaReserva => Pct(Reservado, Ofertado);
            public decimal? Utilizacao => Pct(Utilizado, Ofertado);
            public decimal Ocioso => Math.Max(0, Ofertado - Reservado);
        }

        public static Classe Classificar(CapacidadeVagaLinha l)
        {
            if (!l.TemReserva)
                return l.Status == StatusAgendamento.Cancelado ? Classe.ForaDaOferta : Classe.Livre;

            return l.Status switch
            {
                StatusAgendamento.Cancelado => l.DaGrade ? Classe.Livre : Classe.ForaDaOferta,
                StatusAgendamento.Expirado => Classe.NaoCompareceu,
                StatusAgendamento.EmAndamento or StatusAgendamento.Finalizado => Classe.Realizada,
                _ => Classe.Reservada,
            };
        }

        public static CapacidadeDocasResponseDto Montar(
            IReadOnlyCollection<CapacidadeVagaLinha> linhas,
            IReadOnlyCollection<RelatorioBiDoca> docasAtivas,
            RelatorioBiPeriodoDto periodo)
        {
            var vagas = Preparar(linhas);
            var atual = vagas.Where(v => v.Dia >= periodo.Inicio && v.Dia <= periodo.Fim).ToList();
            var anterior = vagas.Where(v => v.Dia >= periodo.InicioAnterior && v.Dia <= periodo.FimAnterior).ToList();

            var serie = SerieDiaria(atual, periodo.Inicio, periodo.Fim);
            var serieAnterior = SerieDiaria(anterior, periodo.InicioAnterior, periodo.FimAnterior);
            var heatmap = HeatmapSemana(atual);
            var janelas = Janelas(atual);
            var docas = Docas(atual, docasAtivas, periodo);
            var indicadores = Indicadores(atual, anterior, serie, serieAnterior, janelas);
            var excesso = ExcessoDemanda(heatmap);
            var ociosas = Ociosas(heatmap);

            return new CapacidadeDocasResponseDto
            {
                Periodo = periodo,
                Indicadores = indicadores,
                SerieDiaria = serie,
                HeatmapSemana = heatmap,
                HeatmapDocas = HeatmapDocas(atual),
                Docas = docas,
                ExcessoDemanda = excesso,
                Ociosas = ociosas,
                PorHora = PorHora(atual),
                Recomendacoes = Recomendacoes(indicadores, excesso, ociosas, docas),
            };
        }

        private static List<Vaga> Preparar(IReadOnlyCollection<CapacidadeVagaLinha> linhas)
        {
            var classes = linhas.ToDictionary(l => l.Id, Classificar);

            var ciclos = CicloCaminhao
                .Calcular(linhas
                    .Where(l => classes[l.Id] == Classe.Realizada)
                    .Select(l => new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, null)))
                .ToDictionary(c => c.AgendamentoId);

            return linhas
                .Select(l =>
                {
                    var classe = classes[l.Id];
                    DateTime? inicioReal = null;
                    DateTime? fimReal = null;
                    double? descarga = null;

                    if (classe == Classe.Realizada
                        && ciclos.TryGetValue(l.Id, out var ciclo)
                        && ciclo.InicioDescarga.HasValue
                        && l.FimDescargaEm.HasValue
                        && ciclo.DescargaMinutos is > 0)
                    {
                        inicioReal = RelatorioBiPeriodo.HoraLocal(ciclo.InicioDescarga.Value);
                        fimReal = RelatorioBiPeriodo.HoraLocal(l.FimDescargaEm.Value);
                        descarga = ciclo.DescargaMinutos;
                    }

                    return new Vaga(
                        l,
                        classe,
                        RelatorioBiPeriodo.HoraLocal(l.DataInicio),
                        RelatorioBiPeriodo.HoraLocal(l.DataFim),
                        inicioReal,
                        fimReal,
                        descarga);
                })
                .ToList();
        }

        private static IEnumerable<(DateOnly Dia, int Hora, decimal Minutos)> Segmentos(DateTime inicio, DateTime fim)
        {
            var cursor = inicio;
            while (cursor < fim)
            {
                var proximaHora = cursor.Date.AddHours(cursor.Hour + 1);
                var limite = proximaHora < fim ? proximaHora : fim;
                yield return (DateOnly.FromDateTime(cursor), cursor.Hour, (decimal)(limite - cursor).TotalMinutes);
                cursor = limite;
            }
        }

        private static IEnumerable<(DateOnly Dia, int Hora, decimal Minutos)> SegmentosUtilizados(Vaga v) =>
            v.Classe != Classe.Realizada
                ? []
                : v.InicioReal.HasValue && v.FimReal.HasValue
                    ? Segmentos(v.InicioReal.Value, v.FimReal.Value)
                    : Segmentos(v.Inicio, v.Fim);

        private static void Somar(Acumulado acc, Vaga v)
        {
            if (!v.Ofertada)
                return;

            acc.Ofertado += v.MinutosVaga;
            if (v.Reservada)
                acc.Reservado += v.MinutosVaga;
            if (v.Classe == Classe.NaoCompareceu)
                acc.NaoCompareceu += v.MinutosVaga;
            acc.Utilizado += SegmentosUtilizados(v).Sum(s => s.Minutos);
        }

        private static Acumulado Acumular(IEnumerable<Vaga> vagas)
        {
            var acc = new Acumulado();
            foreach (var v in vagas)
                Somar(acc, v);
            return acc;
        }

        private static List<CapacidadeDiaDto> SerieDiaria(List<Vaga> vagas, DateOnly inicio, DateOnly fim)
        {
            var porDia = vagas.GroupBy(v => v.Dia).ToDictionary(g => g.Key, g => Acumular(g));

            return RelatorioBiPeriodo.Dias(inicio, fim)
                .Select(dia =>
                {
                    var acc = porDia.TryGetValue(dia, out var a) ? a : new Acumulado();
                    return new CapacidadeDiaDto
                    {
                        Data = dia,
                        HorasOfertadas = Horas(acc.Ofertado),
                        HorasReservadas = Horas(acc.Reservado),
                        HorasUtilizadas = Horas(acc.Utilizado),
                        HorasNaoComparecimento = Horas(acc.NaoCompareceu),
                        TaxaReservaPct = acc.TaxaReserva,
                        UtilizacaoRealPct = acc.Utilizacao,
                    };
                })
                .ToList();
        }

        private static List<CapacidadeCelulaDto> HeatmapSemana(List<Vaga> vagas)
        {
            var celulas = new List<CapacidadeCelulaDto>();
            var minutos = new Dictionary<(int, int), Acumulado>();

            foreach (var v in vagas.Where(v => v.Ofertada))
            {
                foreach (var (dia, hora, min) in Segmentos(v.Inicio, v.Fim))
                {
                    var acc = Obter(minutos, (DiaDaSemana(dia), hora));
                    acc.Ofertado += min;
                    if (v.Reservada) acc.Reservado += min;
                }

                foreach (var (dia, hora, min) in SegmentosUtilizados(v))
                    Obter(minutos, (DiaDaSemana(dia), hora)).Utilizado += min;
            }

            var janelas = Janelas(vagas)
                .GroupBy(j => (j.DiaSemana, j.Hora))
                .ToDictionary(g => g.Key, g => (Dias: g.Count(), Esgotados: g.Count(j => j.Esgotada)));

            foreach (var grupo in vagas.Where(v => v.Ofertada || v.ForaDaGrade).GroupBy(v => (v.DiaSemana, v.Inicio.Hour)))
            {
                var acc = minutos.TryGetValue(grupo.Key, out var a) ? a : new Acumulado();
                var janela = janelas.TryGetValue(grupo.Key, out var j) ? j : (Dias: 0, Esgotados: 0);
                celulas.Add(new CapacidadeCelulaDto
                {
                    DiaSemana = grupo.Key.DiaSemana,
                    Hora = grupo.Key.Hour,
                    Ofertadas = grupo.Count(v => v.Ofertada),
                    Reservadas = grupo.Count(v => v.ReservaNaGrade),
                    Realizadas = grupo.Count(v => v.Ofertada && v.Classe == Classe.Realizada),
                    NaoCompareceu = grupo.Count(v => v.Ofertada && v.Classe == Classe.NaoCompareceu),
                    ForaDaGrade = grupo.Count(v => v.ForaDaGrade),
                    TaxaReservaPct = acc.TaxaReserva,
                    UtilizacaoRealPct = acc.Utilizacao,
                    DiasComOferta = janela.Dias,
                    DiasEsgotados = janela.Esgotados,
                });
            }

            return celulas.OrderBy(c => c.DiaSemana).ThenBy(c => c.Hora).ToList();
        }

        private static List<CapacidadeDocaHoraDto> HeatmapDocas(List<Vaga> vagas)
        {
            var resultado = new List<CapacidadeDocaHoraDto>();

            foreach (var porDoca in vagas.Where(v => v.Ofertada && v.Linha.LocalDescargaId.HasValue).GroupBy(v => v.Linha.LocalDescargaId!.Value))
            {
                var minutos = new Dictionary<int, Acumulado>();
                foreach (var v in porDoca)
                {
                    foreach (var (_, hora, min) in Segmentos(v.Inicio, v.Fim))
                    {
                        var acc = Obter(minutos, hora);
                        acc.Ofertado += min;
                        if (v.Reservada) acc.Reservado += min;
                    }

                    foreach (var (_, hora, min) in SegmentosUtilizados(v))
                        Obter(minutos, hora).Utilizado += min;
                }

                foreach (var porHora in porDoca.GroupBy(v => v.Inicio.Hour))
                {
                    var acc = minutos.TryGetValue(porHora.Key, out var a) ? a : new Acumulado();
                    resultado.Add(new CapacidadeDocaHoraDto
                    {
                        DocaId = porDoca.Key,
                        Hora = porHora.Key,
                        Ofertadas = porHora.Count(),
                        Reservadas = porHora.Count(v => v.Reservada),
                        TaxaReservaPct = acc.TaxaReserva,
                        UtilizacaoRealPct = acc.Utilizacao,
                    });
                }
            }

            return resultado.OrderBy(r => r.DocaId).ThenBy(r => r.Hora).ToList();
        }

        private sealed record Janela(DateOnly Dia, int DiaSemana, int Hora, bool Esgotada);

        private static List<Janela> Janelas(List<Vaga> vagas) =>
            vagas
                .Where(v => v.Ofertada && v.Linha.DaGrade)
                .GroupBy(v => (v.Dia, v.Inicio.Hour))
                .Select(g => new Janela(g.Key.Dia, DiaDaSemana(g.Key.Dia), g.Key.Hour, g.All(v => v.Reservada)))
                .ToList();

        private static List<CapacidadeDocaDto> Docas(
            List<Vaga> vagas,
            IReadOnlyCollection<RelatorioBiDoca> docasAtivas,
            RelatorioBiPeriodoDto periodo)
        {
            var porDoca = vagas
                .Where(v => v.Linha.LocalDescargaId.HasValue)
                .GroupBy(v => v.Linha.LocalDescargaId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var nomes = docasAtivas.ToDictionary(d => d.Id, d => (d.Nome, d.UnidadeEntregaNome));
            foreach (var (id, lista) in porDoca)
            {
                if (!nomes.ContainsKey(id))
                {
                    var linha = lista[0].Linha;
                    nomes[id] = (linha.LocalDescargaNome ?? "Doca sem nome", linha.UnidadeEntregaNome);
                }
            }

            return nomes
                .Select(par =>
                {
                    var lista = porDoca.TryGetValue(par.Key, out var l) ? l : [];
                    var acc = Acumular(lista);
                    var descargas = lista.Where(v => v.DescargaMinutos.HasValue).Select(v => v.DescargaMinutos!.Value).ToList();
                    var porDia = lista.GroupBy(v => v.Dia).ToDictionary(g => g.Key, g => Acumular(g).TaxaReserva);
                    var pico = lista
                        .Where(v => v.Reservada)
                        .GroupBy(v => v.Inicio.Hour)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key)
                        .Select(g => (int?)g.Key)
                        .FirstOrDefault();

                    return new CapacidadeDocaDto
                    {
                        Id = par.Key,
                        Nome = par.Value.Nome,
                        Unidade = par.Value.UnidadeEntregaNome,
                        VagasOfertadas = lista.Count(v => v.Ofertada),
                        VagasReservadas = lista.Count(v => v.ReservaNaGrade),
                        Descargas = lista.Count(v => v.Classe == Classe.Realizada),
                        NaoCompareceu = lista.Count(v => v.Classe == Classe.NaoCompareceu),
                        ForaDaGrade = lista.Count(v => v.ForaDaGrade),
                        HorasOfertadas = Horas(acc.Ofertado),
                        HorasReservadas = Horas(acc.Reservado),
                        HorasUtilizadas = Horas(acc.Utilizado),
                        HorasOciosas = Horas(acc.Ocioso),
                        TaxaReservaPct = acc.TaxaReserva,
                        UtilizacaoRealPct = acc.Utilizacao,
                        DescargaMediaMin = descargas.Count == 0 ? null : Math.Round((decimal)descargas.Average(), 0),
                        HoraPico = pico,
                        SerieTaxaReserva = RelatorioBiPeriodo.Dias(periodo.Inicio, periodo.Fim)
                            .Select(d => porDia.TryGetValue(d, out var t) ? t : null)
                            .ToList(),
                    };
                })
                .OrderByDescending(d => d.TaxaReservaPct ?? -1)
                .ThenBy(d => d.Nome)
                .ToList();
        }

        private static CapacidadeIndicadoresDto Indicadores(
            List<Vaga> atual,
            List<Vaga> anterior,
            List<CapacidadeDiaDto> serie,
            List<CapacidadeDiaDto> serieAnterior,
            List<Janela> janelas)
        {
            var a = Acumular(atual);
            var b = Acumular(anterior);
            var vagasGrade = atual.Where(v => v.Ofertada && v.Linha.DaGrade).ToList();
            var descargas = atual.Where(v => v.DescargaMinutos.HasValue).Select(v => (decimal)v.DescargaMinutos!.Value).ToList();
            decimal? vagaMedia = vagasGrade.Count == 0 ? null : Math.Round(vagasGrade.Average(v => v.MinutosVaga), 0);
            decimal? descargaMedia = descargas.Count == 0 ? null : Math.Round(descargas.Average(), 0);

            return new CapacidadeIndicadoresDto
            {
                TaxaReservaPct = RelatorioBiPeriodo.Indicador(a.TaxaReserva ?? 0, b.TaxaReserva ?? 0, serie.Where(d => d.TaxaReservaPct.HasValue).Select(d => d.TaxaReservaPct!.Value)),
                UtilizacaoRealPct = RelatorioBiPeriodo.Indicador(a.Utilizacao ?? 0, b.Utilizacao ?? 0, serie.Where(d => d.UtilizacaoRealPct.HasValue).Select(d => d.UtilizacaoRealPct!.Value)),
                HorasOfertadas = RelatorioBiPeriodo.Indicador(Horas(a.Ofertado), Horas(b.Ofertado), serie.Select(d => d.HorasOfertadas)),
                HorasOciosas = RelatorioBiPeriodo.Indicador(Horas(a.Ocioso), Horas(b.Ocioso), serie.Select(d => Math.Max(0, d.HorasOfertadas - d.HorasReservadas))),
                HorasNaoComparecimento = RelatorioBiPeriodo.Indicador(Horas(a.NaoCompareceu), Horas(b.NaoCompareceu), serie.Select(d => d.HorasNaoComparecimento)),
                ForaDaGrade = RelatorioBiPeriodo.Indicador(
                    atual.Count(v => v.ForaDaGrade),
                    anterior.Count(v => v.ForaDaGrade),
                    serie.Select(d => (decimal)atual.Count(v => v.ForaDaGrade && v.Dia == d.Data))),
                VagasOfertadas = atual.Count(v => v.Ofertada),
                VagasReservadas = atual.Count(v => v.ReservaNaGrade),
                JanelasEsgotadas = janelas.Count(j => j.Esgotada),
                JanelasComOferta = janelas.Count,
                VagaMediaMin = vagaMedia,
                DescargaMediaMin = descargaMedia,
                AproveitamentoVagaPct = vagaMedia is > 0 && descargaMedia.HasValue ? Math.Round(descargaMedia.Value * 100m / vagaMedia.Value, 1) : null,
            };
        }

        private static List<CapacidadeHoraDto> PorHora(List<Vaga> vagas)
        {
            var horas = Enumerable.Range(0, 24).Select(_ => new Acumulado()).ToArray();

            foreach (var v in vagas.Where(v => v.Ofertada))
            {
                foreach (var (_, hora, min) in Segmentos(v.Inicio, v.Fim))
                {
                    horas[hora].Ofertado += min;
                    if (v.Reservada) horas[hora].Reservado += min;
                }

                foreach (var (_, hora, min) in SegmentosUtilizados(v))
                    horas[hora].Utilizado += min;
            }

            return Enumerable.Range(0, 24)
                .Select(h => new CapacidadeHoraDto
                {
                    Hora = h,
                    HorasOfertadas = Horas(horas[h].Ofertado),
                    HorasReservadas = Horas(horas[h].Reservado),
                    HorasUtilizadas = Horas(horas[h].Utilizado),
                    TaxaReservaPct = horas[h].TaxaReserva,
                    UtilizacaoRealPct = horas[h].Utilizacao,
                })
                .ToList();
        }

        private static CapacidadeJanelaDto ParaJanela(CapacidadeCelulaDto c) =>
            new()
            {
                DiaSemana = c.DiaSemana,
                Hora = c.Hora,
                Rotulo = RotuloJanela(c.DiaSemana, c.Hora),
                Ofertadas = c.Ofertadas,
                Reservadas = c.Reservadas,
                ForaDaGrade = c.ForaDaGrade,
                TaxaReservaPct = c.TaxaReservaPct,
                DiasComOferta = c.DiasComOferta,
                DiasEsgotados = c.DiasEsgotados,
                VagasLivres = c.Ofertadas - c.Reservadas,
            };

        private static decimal ProporcaoEsgotada(CapacidadeCelulaDto c) =>
            c.DiasComOferta == 0 ? 0 : (decimal)c.DiasEsgotados / c.DiasComOferta;

        private static List<CapacidadeJanelaDto> ExcessoDemanda(List<CapacidadeCelulaDto> heatmap) =>
            heatmap
                .Where(c => c.Ofertadas > 0 && (c.TaxaReservaPct >= LimiteEsgotadoPct || ProporcaoEsgotada(c) >= 0.5m || c.ForaDaGrade >= 2))
                .OrderByDescending(ProporcaoEsgotada)
                .ThenByDescending(c => c.ForaDaGrade)
                .ThenByDescending(c => c.TaxaReservaPct)
                .Take(TopJanelas)
                .Select(c => ParaJanela(c))
                .ToList();

        private static List<CapacidadeJanelaDto> Ociosas(List<CapacidadeCelulaDto> heatmap) =>
            heatmap
                .Where(c => c.Ofertadas > 0 && (c.TaxaReservaPct ?? 0) <= LimiteOciosoPct)
                .OrderByDescending(c => c.Ofertadas - c.Reservadas)
                .ThenBy(c => c.TaxaReservaPct)
                .Take(TopJanelas)
                .Select(ParaJanela)
                .ToList();

        private static List<VisaoGeralDestaqueDto> Recomendacoes(
            CapacidadeIndicadoresDto indicadores,
            List<CapacidadeJanelaDto> excesso,
            List<CapacidadeJanelaDto> ociosas,
            List<CapacidadeDocaDto> docas)
        {
            var recomendacoes = new List<VisaoGeralDestaqueDto>();

            var maisDisputada = excesso.FirstOrDefault(j => j.DiasEsgotados > 0);
            if (maisDisputada is not null)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"{Capitalizar(maisDisputada.Rotulo)} esgotaram em {maisDisputada.DiasEsgotados} de {maisDisputada.DiasComOferta} dias",
                    Descricao = "Todas as vagas desse horário foram reservadas. Abra mais vagas ou outra doca nessa janela.",
                });
            }

            if (indicadores.ForaDaGrade.Valor > 0)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"{indicadores.ForaDaGrade.Valor:0} agendamento{(indicadores.ForaDaGrade.Valor == 1 ? "" : "s")} fora da grade",
                    Descricao = "Caminhões encaixados sem vaga da grade: demanda que não coube na capacidade planejada.",
                });
            }

            var ociosa = ociosas.FirstOrDefault();
            var picoSemFolga = excesso.FirstOrDefault();
            if (ociosa is not null && picoSemFolga is not null)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"Redistribua vagas de {ociosa.Rotulo} para {picoSemFolga.Rotulo}",
                    Descricao = $"{ociosa.VagasLivres} vagas ficaram livres em {ociosa.Rotulo} ({Pct(ociosa.TaxaReservaPct)} reservado), enquanto {picoSemFolga.Rotulo} teve {Pct(picoSemFolga.TaxaReservaPct)}.",
                });
            }
            else if (ociosa is not null)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "info",
                    Titulo = $"{Capitalizar(ociosa.Rotulo)} com só {Pct(ociosa.TaxaReservaPct)} reservado",
                    Descricao = "Horário com sobra de capacidade: dá para reduzir vagas da grade ou concentrar ali os fornecedores flexíveis.",
                });
            }

            var docaOciosa = docas.Where(d => d.VagasOfertadas >= 10 && d.TaxaReservaPct.HasValue).MinBy(d => d.TaxaReservaPct);
            var docaCheia = docas.Where(d => d.VagasOfertadas >= 10 && d.TaxaReservaPct.HasValue).MaxBy(d => d.TaxaReservaPct);
            if (docaOciosa is not null && docaCheia is not null && docaCheia.TaxaReservaPct - docaOciosa.TaxaReservaPct >= 25)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"{docaOciosa.Nome} com {Pct(docaOciosa.TaxaReservaPct)} de reserva",
                    Descricao = $"Bem abaixo de {docaCheia.Nome} ({Pct(docaCheia.TaxaReservaPct)}). Vale revisar quais produtos ou fornecedores podem usar essa doca.",
                });
            }

            if (indicadores.AproveitamentoVagaPct is { } aproveitamento && indicadores.VagaMediaMin.HasValue && indicadores.DescargaMediaMin.HasValue)
            {
                if (aproveitamento <= 70)
                {
                    var ganho = Math.Round((indicadores.VagaMediaMin.Value / indicadores.DescargaMediaMin.Value - 1) * 100m, 0);
                    recomendacoes.Add(new VisaoGeralDestaqueDto
                    {
                        Tipo = "success",
                        Titulo = $"Descarga média de {indicadores.DescargaMediaMin:0} min em vagas de {indicadores.VagaMediaMin:0} min",
                        Descricao = $"Vagas mais curtas abririam até {ganho:0}% mais janelas nas mesmas docas (estimado pelos horários de check-in e finalização).",
                    });
                }
                else if (aproveitamento > 100)
                {
                    recomendacoes.Add(new VisaoGeralDestaqueDto
                    {
                        Tipo = "warning",
                        Titulo = $"Descargas passam da vaga: {indicadores.DescargaMediaMin:0} min em vagas de {indicadores.VagaMediaMin:0} min",
                        Descricao = "O caminhão seguinte tende a esperar. Considere vagas mais longas ou intervalo entre elas.",
                    });
                }
            }

            var ofertadas = indicadores.HorasOfertadas.Valor;
            var perdidas = indicadores.HorasNaoComparecimento.Valor;
            if (ofertadas > 0 && perdidas * 100m / ofertadas >= 5)
            {
                recomendacoes.Add(new VisaoGeralDestaqueDto
                {
                    Tipo = "warning",
                    Titulo = $"{perdidas.ToString("0.#", PtBr)} h de doca perdidas por não comparecimento",
                    Descricao = "Vagas reservadas que ninguém usou. Lembretes e cobrança de confirmação devolvem essa capacidade.",
                });
            }

            return recomendacoes.Take(5).ToList();
        }

        public static string RotuloJanela(int diaSemana, int hora) => $"{DiasSemana[diaSemana]} às {hora}h";

        private static string Capitalizar(string texto) => char.ToUpper(texto[0], PtBr) + texto[1..];

        private static string Pct(decimal? valor) => valor.HasValue ? $"{valor.Value.ToString("0.#", PtBr)}%" : "—";

        private static int DiaDaSemana(DateOnly dia) => ((int)dia.DayOfWeek + 6) % 7;

        private static decimal? Pct(decimal parte, decimal total) =>
            total == 0 ? null : Math.Min(100, Math.Round(parte * 100m / total, 1));

        private static decimal Horas(decimal minutos) => Math.Round(minutos / 60m, 1);

        private static Acumulado Obter<TChave>(Dictionary<TChave, Acumulado> mapa, TChave chave) where TChave : notnull
        {
            if (!mapa.TryGetValue(chave, out var acc))
            {
                acc = new Acumulado();
                mapa[chave] = acc;
            }

            return acc;
        }
    }
}
