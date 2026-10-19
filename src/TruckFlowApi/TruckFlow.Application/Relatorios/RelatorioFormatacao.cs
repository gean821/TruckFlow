using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Relatorios
{
    internal static class RelatorioFormatacao
    {
        public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public static DateTime ParaHorarioLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(utc, Grade.OperationalTimeZone);

        public static string DataHoraLocal(DateTime utc) =>
            ParaHorarioLocal(utc).ToString("dd/MM/yyyy HH:mm", PtBr);
    }
}
