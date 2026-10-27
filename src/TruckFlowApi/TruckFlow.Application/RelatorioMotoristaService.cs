using System.Globalization;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.RelatorioMotorista;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RelatorioMotoristaService : IRelatorioMotoristaService
    {
        public const int MesesHistorico = 6;
        public const int PeriodoMaximoDias = 366;
        private const int TopFatias = 5;

        private static readonly CultureInfo PtBr = new("pt-BR");

        private readonly IAgendamentoRepositorio _repo;
        private readonly CurrentUserGuard _guard;
        private readonly TimeProvider _timeProvider;

        public RelatorioMotoristaService(
            IAgendamentoRepositorio repo,
            CurrentUserGuard guard,
            TimeProvider timeProvider)
        {
            _repo = repo;
            _guard = guard;
            _timeProvider = timeProvider;
        }

        public async Task<RelatorioMotoristaResponseDto> GerarAsync(
            RelatorioMotoristaQueryDto query,
            CancellationToken token = default)
        {
            var motoristaId = _guard.GetMotoristaId();
            var agoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, Grade.OperationalTimeZone));

            var inicio = query.Inicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = query.Fim ?? hoje;

            if (fim < inicio)
                throw new BusinessException("A data final precisa ser igual ou posterior à data inicial.");

            if (fim.DayNumber - inicio.DayNumber + 1 > PeriodoMaximoDias)
                throw new BusinessException($"Escolha um período de até {PeriodoMaximoDias} dias.");

            var (inicioAnterior, _) = PeriodoAnterior(inicio, fim);
            var inicioHistorico = new DateOnly(fim.Year, fim.Month, 1).AddMonths(-(MesesHistorico - 1));

            var desde = inicioAnterior < inicioHistorico ? inicioAnterior : inicioHistorico;

            var linhas = await _repo.ListarParaRelatorioMotoristaAcrossTenants(
                motoristaId,
                InicioDoDiaUtc(desde),
                InicioDoDiaUtc(fim.AddDays(1)),
                token);

            var relatorio = Montar(linhas, inicio, fim);
            relatorio.ProximoAgendamento = await _repo.ObterProximoAgendamentoMotoristaAcrossTenants(motoristaId, agoraUtc, token);

            return relatorio;
        }

        public static RelatorioMotoristaResponseDto Montar(
            IReadOnlyCollection<RelatorioMotoristaLinha> linhas,
            DateOnly inicio,
            DateOnly fim)
        {
            var (inicioAnterior, fimAnterior) = PeriodoAnterior(inicio, fim);

            var doPeriodo = linhas.Where(l => Dentro(l, inicio, fim)).ToList();
            var doAnterior = linhas.Where(l => Dentro(l, inicioAnterior, fimAnterior)).ToList();

            var atual = Resumir(doPeriodo);
            var anterior = Resumir(doAnterior);

            var concluidas = doPeriodo.Where(l => l.Status == StatusAgendamento.Finalizado).ToList();

            return new RelatorioMotoristaResponseDto
            {
                Inicio = inicio,
                Fim = fim,
                InicioAnterior = inicioAnterior,
                FimAnterior = fimAnterior,
                Atual = atual,
                Anterior = anterior,
                VariacaoEntregasPct = Variacao(atual.Concluidas, anterior.Concluidas),
                VariacaoPesoPct = Variacao(atual.PesoKg, anterior.PesoKg),
                HistoricoMensal = Historico(linhas, fim),
                PorProduto = Fatiar(concluidas, l => l.Produto ?? "Sem produto", _ => null),
                PorLocal = Fatiar(concluidas, l => l.UnidadeEntrega ?? "Sem unidade", l => l.Empresa),
            };
        }

        private static (DateOnly Inicio, DateOnly Fim) PeriodoAnterior(DateOnly inicio, DateOnly fim)
        {
            var dias = fim.DayNumber - inicio.DayNumber + 1;
            return (inicio.AddDays(-dias), inicio.AddDays(-1));
        }

        private static DateOnly DiaLocal(DateTime dataUtc) =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc), Grade.OperationalTimeZone));

        private static DateTime InicioDoDiaUtc(DateOnly dia) =>
            TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), Grade.OperationalTimeZone);

        private static bool Dentro(RelatorioMotoristaLinha linha, DateOnly inicio, DateOnly fim)
        {
            var dia = DiaLocal(linha.DataInicio);
            return dia >= inicio && dia <= fim;
        }

        private static RelatorioMotoristaResumoDto Resumir(List<RelatorioMotoristaLinha> linhas)
        {
            var concluidas = linhas.Count(l => l.Status == StatusAgendamento.Finalizado);
            var noPatio = linhas.Count(l => l.Status == StatusAgendamento.EmAndamento);
            var naoCompareceu = linhas.Count(l => l.Status == StatusAgendamento.Expirado);
            var compareceu = concluidas + noPatio;

            return new RelatorioMotoristaResumoDto
            {
                Agendamentos = linhas.Count,
                Concluidas = concluidas,
                NoPatio = noPatio,
                Aguardando = linhas.Count(l => l.Status == StatusAgendamento.Agendado),
                Canceladas = linhas.Count(l => l.Status == StatusAgendamento.Cancelado),
                NaoCompareceu = naoCompareceu,
                PesoKg = linhas.Where(l => l.Status == StatusAgendamento.Finalizado).Sum(l => l.PesoKg),
                ComparecimentoPct = compareceu + naoCompareceu == 0
                    ? null
                    : Math.Round(compareceu * 100m / (compareceu + naoCompareceu), 1),
            };
        }

        private static decimal? Variacao(decimal atual, decimal anterior) =>
            anterior == 0 ? null : Math.Round((atual - anterior) * 100m / anterior, 1);

        private static List<RelatorioMotoristaMesDto> Historico(IEnumerable<RelatorioMotoristaLinha> linhas, DateOnly fim)
        {
            var concluidasPorMes = linhas
                .Where(l => l.Status == StatusAgendamento.Finalizado)
                .GroupBy(l =>
                {
                    var dia = DiaLocal(l.DataInicio);
                    return (dia.Year, dia.Month);
                })
                .ToDictionary(g => g.Key, g => (Entregas: g.Count(), PesoKg: g.Sum(l => l.PesoKg)));

            var primeiroMes = new DateOnly(fim.Year, fim.Month, 1).AddMonths(-(MesesHistorico - 1));

            return Enumerable.Range(0, MesesHistorico)
                .Select(i => primeiroMes.AddMonths(i))
                .Select(mes =>
                {
                    concluidasPorMes.TryGetValue((mes.Year, mes.Month), out var totais);
                    return new RelatorioMotoristaMesDto
                    {
                        Ano = mes.Year,
                        Mes = mes.Month,
                        Rotulo = PtBr.DateTimeFormat.GetAbbreviatedMonthName(mes.Month).TrimEnd('.'),
                        Entregas = totais.Entregas,
                        PesoKg = totais.PesoKg,
                    };
                })
                .ToList();
        }

        private static List<RelatorioMotoristaFatiaDto> Fatiar(
            List<RelatorioMotoristaLinha> concluidas,
            Func<RelatorioMotoristaLinha, string> nome,
            Func<RelatorioMotoristaLinha, string?> detalhe)
        {
            var pesoTotal = concluidas.Sum(l => l.PesoKg);

            var grupos = concluidas
                .GroupBy(l => (Nome: nome(l), Detalhe: detalhe(l)))
                .Select(g => new RelatorioMotoristaFatiaDto
                {
                    Nome = g.Key.Nome,
                    Detalhe = g.Key.Detalhe,
                    Entregas = g.Count(),
                    PesoKg = g.Sum(l => l.PesoKg),
                })
                .OrderByDescending(f => f.PesoKg)
                .ThenByDescending(f => f.Entregas)
                .ThenBy(f => f.Nome)
                .ToList();

            if (grupos.Count > TopFatias)
            {
                var outros = grupos.Skip(TopFatias - 1).ToList();
                grupos = grupos.Take(TopFatias - 1).ToList();
                grupos.Add(new RelatorioMotoristaFatiaDto
                {
                    Nome = "Outros",
                    Entregas = outros.Sum(f => f.Entregas),
                    PesoKg = outros.Sum(f => f.PesoKg),
                });
            }

            foreach (var fatia in grupos)
            {
                fatia.Percentual = pesoTotal == 0
                    ? Math.Round(fatia.Entregas * 100m / concluidas.Count, 1)
                    : Math.Round(fatia.PesoKg * 100m / pesoTotal, 1);
            }

            return grupos;
        }
    }
}
