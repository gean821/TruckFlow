namespace TruckFlow.Application.Validators.Grade
{
    /// <summary>
    /// Regra compartilhada entre o validator de criação e o GradeService (update, após o merge
    /// parcial do PATCH): garante que o intervalo em minutos cabe na janela HoraInicial→HoraFinal,
    /// usando a mesma semântica de virada de meia-noite do motor de geração de slots
    /// (<see cref="TruckFlow.Domain.Entities.Grade.GerarSlots"/>).
    /// </summary>
    public static class GradeJanelaRules
    {
        public static bool IntervaloCabeNaJanela(TimeOnly horaInicial, TimeOnly horaFinal, int intervaloMinutos)
        {
            var duracao = horaFinal.ToTimeSpan() - horaInicial.ToTimeSpan();

            if (duracao <= TimeSpan.Zero)
            {
                duracao += TimeSpan.FromDays(1);
            }

            return TimeSpan.FromMinutes(intervaloMinutos) <= duracao;
        }
    }
}
