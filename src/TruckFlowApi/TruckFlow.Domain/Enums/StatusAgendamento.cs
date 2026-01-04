namespace TruckFlow.Domain.Enums
{
    public enum StatusAgendamento
    {
        Disponivel = 0,   // vaga aberta
        Pendente = 1,     // (opcional, se usar pré-validação)
        Agendado = 2,     // motorista reservou
        EmAndamento = 3, // check-in realizado (na doca)
        Finalizado = 4,  // check-out realizado
        Cancelado = 5
    }
}
