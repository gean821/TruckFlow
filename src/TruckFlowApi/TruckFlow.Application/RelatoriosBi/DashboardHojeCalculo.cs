using System.Globalization;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class DashboardHojeCalculo
    {
        public const int SemanasReferencia = 4;
        public const int JanelaProximasHoras = 2;
        private static readonly CultureInfo PtBr = new("pt-BR");

        public static DashboardHojeResponseDto Montar(
            IReadOnlyCollection<RelatorioBiLinha> linhas,
            IReadOnlyCollection<RelatorioBiDoca> docas,
            DateTime agoraUtc,
            decimal? metaDiariaKg)
        {
            var agoraLocal = RelatorioBiPeriodo.HoraLocal(agoraUtc);
            var hoje = DateOnly.FromDateTime(agoraLocal);
            var horaDoDia = agoraLocal.TimeOfDay;

            var doDia = linhas.Where(l => RelatorioBiPeriodo.DiaLocal(l.DataInicio) == hoje).ToList();
            var referencias = Enumerable.Range(1, SemanasReferencia)
                .Select(s => hoje.AddDays(-7 * s))
                .Select(dia => linhas.Where(l => RelatorioBiPeriodo.DiaLocal(l.DataInicio) == dia).ToList())
                .ToList();

            var noPatio = linhas.Where(l => l.Status == StatusAgendamento.EmAndamento).ToList();
            var atrasados = doDia
                .Where(l => l.Status == StatusAgendamento.Agendado && l.DataInicio < agoraUtc)
                .OrderBy(l => l.DataInicio)
                .ToList();

            var concluidosHoje = doDia.Where(l => l.Status == StatusAgendamento.Finalizado).ToList();
            var pesoHoje = concluidosHoje.Sum(l => l.PesoKg);

            var indicadores = new DashboardHojeIndicadoresDto
            {
                AgendadosHoje = Indicador(doDia.Count(Ativo), referencias, r => r.Count(Ativo)),
                Concluidos = Indicador(concluidosHoje.Count, referencias, r => AteAgora(r, horaDoDia).Count(l => l.Status == StatusAgendamento.Finalizado)),
                NoPatio = new DashboardHojeIndicadorDto { Valor = noPatio.Count },
                Atrasados = new DashboardHojeIndicadorDto { Valor = atrasados.Count },
                NaoCompareceu = Indicador(doDia.Count(l => l.Status == StatusAgendamento.Expirado), referencias, r => AteAgora(r, horaDoDia).Count(l => l.Status == StatusAgendamento.Expirado)),
                PesoRecebidoKg = Indicador(pesoHoje, referencias, r => AteAgora(r, horaDoDia).Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg)),
                AindaPorVir = doDia.Count(l => l.Status == StatusAgendamento.Agendado && l.DataInicio >= agoraUtc),
                AtingimentoMetaPct = metaDiariaKg is > 0 ? Math.Round(pesoHoje * 100m / metaDiariaKg.Value, 1) : null,
            };

            return new DashboardHojeResponseDto
            {
                AgoraUtc = agoraUtc,
                Dia = hoje,
                HoraAtual = agoraLocal.Hour,
                DiaSemana = PtBr.DateTimeFormat.GetDayName(agoraLocal.DayOfWeek),
                Indicadores = indicadores,
                MetaDiariaKg = metaDiariaKg,
                PorHora = PorHora(doDia, referencias),
                Docas = Docas(docas, linhas, doDia, agoraUtc),
                Patio = Patio(noPatio, concluidosHoje, linhas, agoraUtc),
                ProximasChegadas = doDia
                    .Where(l => l.Status == StatusAgendamento.Agendado && l.DataInicio >= agoraUtc && l.DataInicio < agoraUtc.AddHours(JanelaProximasHoras))
                    .OrderBy(l => l.DataInicio)
                    .Select(l => Caminhao(l, (int)Math.Round((l.DataInicio - agoraUtc).TotalMinutes)))
                    .ToList(),
                Atrasados = atrasados
                    .Select(l => Caminhao(l, (int)Math.Round((agoraUtc - l.DataInicio).TotalMinutes)))
                    .ToList(),
            };
        }

        private static bool Ativo(RelatorioBiLinha l) => l.Status != StatusAgendamento.Cancelado;

        private static IEnumerable<RelatorioBiLinha> AteAgora(IEnumerable<RelatorioBiLinha> linhas, TimeSpan horaDoDia) =>
            linhas.Where(l => RelatorioBiPeriodo.HoraLocal(l.DataInicio).TimeOfDay <= horaDoDia);

        private static DashboardHojeIndicadorDto Indicador(
            decimal valor,
            List<List<RelatorioBiLinha>> referencias,
            Func<List<RelatorioBiLinha>, decimal> medida)
        {
            var comMovimento = referencias.Where(r => r.Count > 0).ToList();
            if (comMovimento.Count == 0)
            {
                return new DashboardHojeIndicadorDto { Valor = valor };
            }

            var media = Math.Round(comMovimento.Average(medida), 1);

            return new DashboardHojeIndicadorDto
            {
                Valor = valor,
                Referencia = media,
                VariacaoPct = RelatorioBiPeriodo.Variacao(valor, media),
            };
        }

        private static List<DashboardHojeHoraDto> PorHora(List<RelatorioBiLinha> doDia, List<List<RelatorioBiLinha>> referencias)
        {
            var semanas = Math.Max(1, referencias.Count(r => r.Count > 0));

            return Enumerable.Range(0, 24)
                .Select(h => new DashboardHojeHoraDto
                {
                    Hora = h,
                    Agendados = doDia.Count(l => Ativo(l) && RelatorioBiPeriodo.HoraLocal(l.DataInicio).Hour == h),
                    Chegadas = doDia.Count(l => l.ChegouEm.HasValue && RelatorioBiPeriodo.HoraLocal(l.ChegouEm.Value).Hour == h),
                    Concluidos = doDia.Count(l => l.Status == StatusAgendamento.Finalizado
                        && RelatorioBiPeriodo.HoraLocal(l.FimDescargaEm ?? l.DataFim).Hour == h),
                    MediaAgendados = Math.Round(
                        referencias.Sum(r => r.Count(l => Ativo(l) && RelatorioBiPeriodo.HoraLocal(l.DataInicio).Hour == h)) / (decimal)semanas,
                        1),
                })
                .ToList();
        }

        private static List<DashboardHojeDocaDto> Docas(
            IReadOnlyCollection<RelatorioBiDoca> docas,
            IReadOnlyCollection<RelatorioBiLinha> linhas,
            List<RelatorioBiLinha> doDia,
            DateTime agoraUtc)
        {
            return docas
                .Select(doca =>
                {
                    var atual = linhas
                        .Where(l => l.LocalDescargaId == doca.Id && l.Status == StatusAgendamento.EmAndamento)
                        .OrderBy(l => l.ChegouEm ?? l.DataInicio)
                        .FirstOrDefault();

                    var proximo = doDia
                        .Where(l => l.LocalDescargaId == doca.Id && l.Status == StatusAgendamento.Agendado && l.DataFim > agoraUtc)
                        .OrderBy(l => l.DataInicio)
                        .FirstOrDefault();

                    return new DashboardHojeDocaDto
                    {
                        Id = doca.Id,
                        Nome = doca.Nome,
                        Unidade = doca.UnidadeEntregaNome,
                        Ocupada = atual is not null,
                        Atual = atual is null ? null : Caminhao(atual, Minutos(atual.ChegouEm ?? atual.DataInicio, agoraUtc)),
                        Proximo = proximo is null ? null : Caminhao(proximo, (int)Math.Round((proximo.DataInicio - agoraUtc).TotalMinutes)),
                        ConcluidosHoje = doDia.Count(l => l.LocalDescargaId == doca.Id && l.Status == StatusAgendamento.Finalizado),
                    };
                })
                .ToList();
        }

        private static DashboardHojePatioDto Patio(
            List<RelatorioBiLinha> noPatio,
            List<RelatorioBiLinha> concluidosHoje,
            IReadOnlyCollection<RelatorioBiLinha> linhas,
            DateTime agoraUtc)
        {
            var ciclos = CicloCaminhao.Calcular(linhas.Select(l => new CicloCaminhaoEntrada(
                    l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm)))
                .ToDictionary(c => c.AgendamentoId);

            var deHoje = concluidosHoje
                .Select(l => ciclos.TryGetValue(l.Id, out var c) ? c : null)
                .Where(c => c is not null)
                .ToList();

            var maisAntigo = noPatio.OrderBy(l => l.ChegouEm ?? l.DataInicio).FirstOrDefault();

            return new DashboardHojePatioDto
            {
                NoPatio = noPatio.Count,
                PermanenciaMediaMin = Media(deHoje.Select(c => c!.PermanenciaMinutos)),
                DescargaMediaMin = Media(deHoje.Select(c => c!.DescargaMinutos)),
                EsperaMediaMin = Media(deHoje.Select(c => c!.EsperaMinutos)),
                MaisAntigo = maisAntigo is null ? null : Caminhao(maisAntigo, Minutos(maisAntigo.ChegouEm ?? maisAntigo.DataInicio, agoraUtc)),
            };
        }

        private static double? Media(IEnumerable<double?> valores)
        {
            var lista = valores.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return lista.Count == 0 ? null : Math.Round(lista.Average(), 0);
        }

        private static int Minutos(DateTime desdeUtc, DateTime agoraUtc) =>
            Math.Max(0, (int)Math.Round((agoraUtc - desdeUtc).TotalMinutes));

        private static DashboardHojeCaminhaoDto Caminhao(RelatorioBiLinha l, int? minutos) =>
            new()
            {
                AgendamentoId = l.Id,
                DataInicio = l.DataInicio,
                Placa = l.PlacaVeiculo,
                Motorista = l.MotoristaNome,
                Fornecedor = l.FornecedorNome,
                Produto = l.ProdutoNome,
                Doca = l.LocalDescargaNome,
                PesoKg = l.PesoKg,
                Minutos = minutos,
            };
    }
}
