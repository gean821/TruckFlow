using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Application.Relatorios
{
    public static class RelatorioDetalhadoCalculo
    {
        public static string StatusRotulo(StatusAgendamento status) => status switch
        {
            StatusAgendamento.Disponivel => "Disponível",
            StatusAgendamento.Pendente => "Pendente",
            StatusAgendamento.Agendado => "Agendado",
            StatusAgendamento.EmAndamento => "No pátio",
            StatusAgendamento.Finalizado => "Concluído",
            StatusAgendamento.Cancelado => "Cancelado",
            StatusAgendamento.Expirado => "Não compareceu",
            _ => status.ToString()
        };

        public static string? TipoVeiculoRotulo(TipoVeiculo? tipo) => tipo switch
        {
            null => null,
            TipoVeiculo.CarretaDoisEixos => "Carreta 2 eixos",
            TipoVeiculo.Ls => "LS (cavalo mecânico)",
            TipoVeiculo.CarretaCavaloTrucado => "Carreta cavalo trucado",
            TipoVeiculo.BiTrem => "Bitrem",
            TipoVeiculo.TriTrem => "Tritrem",
            TipoVeiculo.RodoTrem => "Rodotrem",
            TipoVeiculo.CarretaBau => "Carreta baú",
            TipoVeiculo.CarretaSider => "Carreta sider",
            TipoVeiculo.Vanderleia => "Vanderléia",
            TipoVeiculo.BauFrigorifico => "Baú frigorífico",
            _ => tipo.ToString()
        };

        public static SituacaoChegada? Situacao(StatusAgendamento status, DateTime inicio, DateTime fim, DateTime? chegouEm)
        {
            if (chegouEm is null)
            {
                return status == StatusAgendamento.Expirado ? SituacaoChegada.NaoCompareceu : null;
            }

            if (chegouEm < inicio)
            {
                return SituacaoChegada.Adiantado;
            }

            return chegouEm <= fim ? SituacaoChegada.NoHorario : SituacaoChegada.Atrasado;
        }

        public static string? SituacaoRotulo(SituacaoChegada? situacao) => situacao switch
        {
            SituacaoChegada.Adiantado => "Adiantado",
            SituacaoChegada.NoHorario => "No horário",
            SituacaoChegada.Atrasado => "Atrasado",
            SituacaoChegada.NaoCompareceu => "Não compareceu",
            _ => null
        };

        public static IReadOnlyDictionary<Guid, List<DateTime>> FinsPorDoca(IEnumerable<FimDescargaDoca> fins) =>
            CicloCaminhao.FinsPorDoca(fins.Select(f => (f.LocalDescargaId, f.FimDescargaEm)));

        public static CicloCaminhaoResultado Ciclo(AgendamentoRelatorioLinha l, IReadOnlyDictionary<Guid, List<DateTime>> fins) =>
            CicloCaminhao.Calcular(
                new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm),
                fins);

        public static double? AtrasoChegadaMin(DateTime inicio, DateTime? chegouEm) =>
            chegouEm.HasValue ? Math.Round((chegouEm.Value - inicio).TotalMinutes, 1) : null;

        public static AgendamentoDetalhadoDto Mapear(AgendamentoRelatorioLinha l)
        {
            var ciclo = l.Ciclo;

            return new AgendamentoDetalhadoDto
            {
                Id = l.Id,
                DataInicio = l.DataInicio,
                DataFim = l.DataFim,
                Status = l.Status.ToString(),
                StatusRotulo = StatusRotulo(l.Status),
                Origem = l.DaGrade ? nameof(OrigemAgendamento.Grade) : nameof(OrigemAgendamento.Avulso),
                MotoristaNome = l.MotoristaNome,
                MotoristaTelefone = l.MotoristaTelefone,
                PlacaVeiculo = l.PlacaVeiculo,
                TipoVeiculo = TipoVeiculoRotulo(l.TipoVeiculo),
                FornecedorNome = l.FornecedorNome,
                Produto = l.Produto,
                UnidadeEntrega = l.UnidadeEntrega,
                LocalDescarga = l.LocalDescarga,
                PesoKg = l.PesoCarga,
                NotaFiscalNumero = l.NotaFiscalNumero,
                ChegouEm = l.ChegouEm,
                InicioDescarga = ciclo?.InicioDescarga,
                InicioEstimado = ciclo?.InicioEstimado ?? false,
                FimDescargaEm = l.FimDescargaEm,
                Saida = ciclo?.Saida,
                SaidaEstimada = ciclo?.SaidaEstimada ?? false,
                EsperaMin = ciclo?.EsperaMinutos,
                DescargaMin = ciclo?.DescargaMinutos,
                PermanenciaMin = ciclo?.PermanenciaMinutos,
                AtrasoChegadaMin = AtrasoChegadaMin(l.DataInicio, l.ChegouEm),
                SituacaoChegada = Situacao(l.Status, l.DataInicio, l.DataFim, l.ChegouEm)?.ToString()
            };
        }

        public static RelatorioDetalhadoResumoDto Resumo(
            IReadOnlyList<RelatorioResumoLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins)
        {
            var ciclos = linhas
                .Select(l => CicloCaminhao.Calcular(
                    new CicloCaminhaoEntrada(l.Id, l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm, l.SaiuEm),
                    fins))
                .ToList();

            var situacoes = linhas
                .Select(l => (Linha: l, Situacao: Situacao(l.Status, l.DataInicio, l.DataFim, l.ChegouEm)))
                .ToList();

            int Contar(SituacaoChegada s) => situacoes.Count(x => x.Situacao == s);

            var chegaram = linhas.Count(l => l.ChegouEm.HasValue);
            var adiantados = Contar(SituacaoChegada.Adiantado);
            var noHorario = Contar(SituacaoChegada.NoHorario);
            var atrasados = situacoes.Where(x => x.Situacao == SituacaoChegada.Atrasado).ToList();

            var esperas = ciclos.Where(c => c.EsperaMinutos.HasValue).Select(c => c.EsperaMinutos!.Value).ToList();

            return new RelatorioDetalhadoResumoDto
            {
                Total = linhas.Count,
                PesoTotalKg = linhas.Sum(l => l.PesoKg),
                PorStatus = linhas
                    .GroupBy(l => l.Status)
                    .OrderBy(g => g.Key)
                    .Select(g => new RelatorioDetalhadoStatusDto
                    {
                        Status = g.Key.ToString(),
                        Rotulo = StatusRotulo(g.Key),
                        Quantidade = g.Count()
                    })
                    .ToList(),
                Chegaram = chegaram,
                Adiantados = adiantados,
                NoHorario = noHorario,
                Atrasados = atrasados.Count,
                NaoCompareceram = Contar(SituacaoChegada.NaoCompareceu),
                PontualidadePct = chegaram > 0 ? Math.Round((adiantados + noHorario) * 100m / chegaram, 1) : null,
                AtrasoMedioMin = Media(atrasados.Select(x => AtrasoChegadaMin(x.Linha.DataInicio, x.Linha.ChegouEm)!.Value)),
                EsperaMediaMin = Media(esperas),
                EsperaP90Min = Percentil(esperas, 0.9),
                DescargaMediaMin = Media(ciclos.Where(c => c.DescargaMinutos.HasValue).Select(c => c.DescargaMinutos!.Value)),
                PermanenciaMediaMin = Media(ciclos.Where(c => c.PermanenciaMinutos.HasValue).Select(c => c.PermanenciaMinutos!.Value)),
                TemposEstimados = ciclos.Any(c => c.InicioEstimado || c.SaidaEstimada)
            };
        }

        public static double? Percentil(IReadOnlyCollection<double> valores, double p)
        {
            if (valores.Count == 0)
            {
                return null;
            }

            var ordenados = valores.OrderBy(v => v).ToList();
            var posicao = (int)Math.Ceiling(p * ordenados.Count) - 1;
            return Math.Round(ordenados[Math.Clamp(posicao, 0, ordenados.Count - 1)], 1);
        }

        private static double? Media(IEnumerable<double> valores)
        {
            var lista = valores.ToList();
            return lista.Count > 0 ? Math.Round(lista.Average(), 1) : null;
        }
    }
}
