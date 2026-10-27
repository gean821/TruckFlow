namespace TruckFlow.Domain.Rules
{
    public sealed record CicloCaminhaoEntrada(
        Guid AgendamentoId,
        Guid? LocalDescargaId,
        DateTime? ChegouEm,
        DateTime? InicioDescargaEm,
        DateTime? FimDescargaEm,
        DateTime? SaiuEm);

    public sealed record CicloCaminhaoResultado(
        Guid AgendamentoId,
        DateTime? InicioDescarga,
        bool InicioEstimado,
        DateTime? Saida,
        bool SaidaEstimada,
        double? EsperaMinutos,
        double? DescargaMinutos,
        double? PermanenciaMinutos);

    public static class CicloCaminhao
    {
        public static IReadOnlyList<CicloCaminhaoResultado> Calcular(IEnumerable<CicloCaminhaoEntrada> agendamentos)
        {
            var lista = agendamentos.ToList();

            var finsPorDoca = FinsPorDoca(lista
                .Where(a => a.LocalDescargaId.HasValue && a.FimDescargaEm.HasValue)
                .Select(a => (a.LocalDescargaId!.Value, a.FimDescargaEm!.Value)));

            return lista.Select(a => Calcular(a, finsPorDoca)).ToList();
        }

        public static IReadOnlyDictionary<Guid, List<DateTime>> FinsPorDoca(IEnumerable<(Guid Doca, DateTime Fim)> fins) =>
            fins
                .GroupBy(f => f.Doca)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Fim).OrderBy(f => f).ToList());

        public static CicloCaminhaoResultado Calcular(
            CicloCaminhaoEntrada a,
            IReadOnlyDictionary<Guid, List<DateTime>> finsPorDoca)
        {
            var inicio = a.InicioDescargaEm;
            var inicioEstimado = false;

            if (inicio is null && a.ChegouEm.HasValue && a.FimDescargaEm.HasValue)
            {
                var fimAnteriorNaDoca = AnteriorNaDoca(a, finsPorDoca);
                inicio = Maior(a.ChegouEm.Value, fimAnteriorNaDoca);
                if (inicio > a.FimDescargaEm)
                {
                    inicio = a.FimDescargaEm;
                }

                inicioEstimado = true;
            }

            var saida = a.SaiuEm ?? a.FimDescargaEm;
            var saidaEstimada = a.SaiuEm is null && a.FimDescargaEm.HasValue;

            return new CicloCaminhaoResultado(
                a.AgendamentoId,
                inicio,
                inicioEstimado,
                saida,
                saidaEstimada,
                Minutos(a.ChegouEm, inicio),
                Minutos(inicio, a.FimDescargaEm),
                Minutos(a.ChegouEm, saida));
        }

        private static DateTime? AnteriorNaDoca(
            CicloCaminhaoEntrada a,
            IReadOnlyDictionary<Guid, List<DateTime>> finsPorDoca)
        {
            if (!a.LocalDescargaId.HasValue || !finsPorDoca.TryGetValue(a.LocalDescargaId.Value, out var fins))
            {
                return null;
            }

            var indice = fins.BinarySearch(a.FimDescargaEm!.Value);
            var primeiroMaiorOuIgual = indice >= 0 ? PrimeiraOcorrencia(fins, indice) : ~indice;

            return primeiroMaiorOuIgual > 0 ? fins[primeiroMaiorOuIgual - 1] : null;
        }

        private static int PrimeiraOcorrencia(List<DateTime> fins, int indice)
        {
            while (indice > 0 && fins[indice - 1] == fins[indice])
            {
                indice--;
            }

            return indice;
        }

        private static DateTime Maior(DateTime a, DateTime? b) => b.HasValue && b.Value > a ? b.Value : a;

        private static double? Minutos(DateTime? de, DateTime? ate) =>
            de.HasValue && ate.HasValue && ate.Value >= de.Value
                ? Math.Round((ate.Value - de.Value).TotalMinutes, 1)
                : null;
    }
}
