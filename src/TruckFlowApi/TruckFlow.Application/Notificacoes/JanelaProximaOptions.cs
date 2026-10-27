namespace TruckFlow.Application.Notificacoes
{
    public sealed class JanelaProximaOptions
    {
        public const string SectionName = "Notificacoes:JanelaProxima";

        public int AntecedenciaMinutos { get; set; } = 60;

        public int LoteMaximo { get; set; } = 200;
    }
}
