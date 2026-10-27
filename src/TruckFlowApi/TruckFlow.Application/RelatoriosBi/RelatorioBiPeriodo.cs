using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.RelatoriosBi
{
    public static class RelatorioBiPeriodo
    {
        public const int PeriodoMaximoDias = 366;

        public static RelatorioBiPeriodoDto Resolver(RelatorioBiFiltroDto filtro, DateTime agoraUtc)
        {
            var hoje = DiaLocal(agoraUtc);
            var inicio = filtro.Inicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = filtro.Fim ?? hoje;

            if (fim < inicio)
                throw new BusinessException("A data final precisa ser igual ou posterior à data inicial.");

            var dias = fim.DayNumber - inicio.DayNumber + 1;
            if (dias > PeriodoMaximoDias)
                throw new BusinessException($"Escolha um período de até {PeriodoMaximoDias} dias.");

            return new RelatorioBiPeriodoDto
            {
                Inicio = inicio,
                Fim = fim,
                InicioAnterior = inicio.AddDays(-dias),
                FimAnterior = inicio.AddDays(-1),
                Dias = dias,
            };
        }

        public static RelatorioBiCriteria Criteria(RelatorioBiFiltroDto filtro, RelatorioBiPeriodoDto periodo) =>
            new(
                InicioDoDiaUtc(periodo.InicioAnterior),
                InicioDoDiaUtc(periodo.Fim.AddDays(1)),
                filtro.UnidadeEntregaId,
                filtro.LocalDescargaId,
                filtro.FornecedorId,
                filtro.ProdutoId);

        public static DateOnly DiaLocal(DateTime dataUtc) =>
            DateOnly.FromDateTime(HoraLocal(dataUtc));

        public static DateTime HoraLocal(DateTime dataUtc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc), Grade.OperationalTimeZone);

        public static DateTime InicioDoDiaUtc(DateOnly dia) =>
            TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), Grade.OperationalTimeZone);

        public static bool Dentro(DateTime dataUtc, DateOnly inicio, DateOnly fim)
        {
            var dia = DiaLocal(dataUtc);
            return dia >= inicio && dia <= fim;
        }

        public static IEnumerable<DateOnly> Dias(DateOnly inicio, DateOnly fim)
        {
            for (var dia = inicio; dia <= fim; dia = dia.AddDays(1))
                yield return dia;
        }

        public static decimal? Variacao(decimal atual, decimal anterior) =>
            anterior == 0 ? null : Math.Round((atual - anterior) * 100m / anterior, 1);

        public static RelatorioBiIndicadorDto Indicador(decimal valor, decimal anterior, IEnumerable<decimal> serie) =>
            new()
            {
                Valor = valor,
                Anterior = anterior,
                VariacaoPct = Variacao(valor, anterior),
                Serie = serie.ToList(),
            };
    }
}
