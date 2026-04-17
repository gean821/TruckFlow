using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public sealed class Grade : EntidadeBase, IEmpresaScoped
    {
        public required Produto Produto { get; set; }
        public required Guid ProdutoId { get; set; }
        public UnidadeEntrega? UnidadeEntrega { get; set; }
        public LocalDescarga? LocalDescarga { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public  Guid? UnidadeEntregaId { get; set; }
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public DateOnly DataInicio { get; set; }
        public DateOnly DataFim { get; set; }
        public TimeOnly HoraInicial { get; set; }
        public TimeOnly HoraFinal { get; set; }
        public int IntervaloMinutos { get; set; }
        public string DiasSemana { get; set; } = string.Empty;

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public ICollection<Agendamento> Agendamentos { get; set; } = [];

        private static readonly TimeZoneInfo OperationalTimeZone = ResolveOperationalTimeZone();

        private static TimeZoneInfo ResolveOperationalTimeZone()
        {
            foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Utc;
        }

        public List<Agendamento> GerarSlots()
        {
            var slots = new List<Agendamento>();

            var diaAtual = DataInicio.ToDateTime(TimeOnly.MinValue);
            var diaFim = DataFim.ToDateTime(TimeOnly.MinValue);

            var diasPermitidos = DiasSemanaEnum;
            var intervalo = TimeSpan.FromMinutes(IntervaloMinutos);

            while (diaAtual <= diaFim)
            {
                if (diasPermitidos.Contains(diaAtual.DayOfWeek))
                {
                    var horaAtual = diaAtual.Add(HoraInicial.ToTimeSpan());
                    var horaLimite = diaAtual.Add(HoraFinal.ToTimeSpan());

                    if (horaLimite <= horaAtual)
                    {
                        horaLimite = horaLimite.AddDays(1);
                    }

                    while (horaAtual.Add(intervalo) <= horaLimite)
                    {
                        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(horaAtual, DateTimeKind.Unspecified),
                            OperationalTimeZone);

                        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(horaAtual.Add(intervalo), DateTimeKind.Unspecified),
                            OperationalTimeZone);

                        slots.Add(new Agendamento
                        {
                            GradeId = Id,
                            Fornecedor = Fornecedor,
                            FornecedorId = FornecedorId,
                            UnidadeEntrega = UnidadeEntrega,
                            UnidadeEntregaId = UnidadeEntregaId,
                            EmpresaId = EmpresaId,
                            DataInicio = inicioUtc,
                            DataFim = fimUtc,
                            StatusAgendamento = StatusAgendamento.Disponivel,
                            TipoCarga = TipoCarga.Indefinido,
                            CreatedAt = DateTime.UtcNow
                        });

                        horaAtual = horaAtual.Add(intervalo);
                    }
                }

                diaAtual = diaAtual.AddDays(1);
            }

            return slots;
        }

        public IReadOnlyCollection<DayOfWeek> DiasSemanaEnum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DiasSemana))
                {
                    return Enum.GetValues<DayOfWeek>();
                }

                return DiasSemana
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (DayOfWeek)int.Parse(x))
                    .ToList();
            }
        }
        public void DefinirDiasSemana(IEnumerable<DayOfWeek> dias)
        {
            DiasSemana = string.Join(",", dias.Select(d => (int)d));
        }
    }
}
