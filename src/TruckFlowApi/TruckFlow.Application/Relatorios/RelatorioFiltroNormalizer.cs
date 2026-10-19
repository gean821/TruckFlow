using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Relatorios
{
    public static class RelatorioFiltroNormalizer
    {
        public static RelatorioAgendamentoCriteria Normalizar(RelatorioAgendamentoFilterDto filtros)
        {
            if (!filtros.DataInicio.HasValue || !filtros.DataFim.HasValue)
            {
                throw new BusinessException("Informe o período (data início e data fim) para gerar o relatório.");
            }

            var diaInicio = filtros.DataInicio.Value.Date;
            var diaFim = filtros.DataFim.Value.Date;

            if (diaFim < diaInicio)
            {
                throw new BusinessException("A data fim não pode ser anterior à data início.");
            }

            var diasNoPeriodo = (diaFim - diaInicio).Days + 1;
            if (diasNoPeriodo > RelatorioLimites.PeriodoMaximoDias)
            {
                throw new BusinessException(
                    $"O período máximo do relatório é de {RelatorioLimites.PeriodoMaximoDias} dias.");
            }

            ValidarHora(filtros.HoraInicio, "início");
            ValidarHora(filtros.HoraFim, "fim");

            if (filtros.HoraInicio.HasValue && filtros.HoraFim.HasValue
                && filtros.HoraInicio.Value > filtros.HoraFim.Value)
            {
                throw new BusinessException("A hora início não pode ser maior que a hora fim.");
            }

            return new RelatorioAgendamentoCriteria
            {
                DataInicioUtc = ParaUtc(diaInicio),
                DataFimUtc = ParaUtc(diaFim.AddDays(1).AddTicks(-1)),
                HoraInicio = filtros.HoraInicio,
                HoraFim = filtros.HoraFim,
                FornecedorId = filtros.FornecedorId,
                ProdutoId = filtros.ProdutoId,
                PlacaVeiculo = Limpar(filtros.PlacaVeiculo),
                Motorista = Limpar(filtros.Motorista)
            };
        }

        private static void ValidarHora(TimeSpan? hora, string nome)
        {
            if (hora.HasValue && (hora.Value < TimeSpan.Zero || hora.Value >= TimeSpan.FromDays(1)))
            {
                throw new BusinessException($"Hora {nome} inválida.");
            }
        }

        private static DateTime ParaUtc(DateTime horarioLocal) =>
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(horarioLocal, DateTimeKind.Unspecified),
                Grade.OperationalTimeZone);

        private static string? Limpar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
