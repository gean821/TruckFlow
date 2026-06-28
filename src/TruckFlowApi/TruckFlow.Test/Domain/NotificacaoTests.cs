using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Domain;

public class NotificacaoTests
{
    private static Notificacao CriarNotificacao(bool jaLida = false)
    {
        var n = new Notificacao
        {
            EmpresaId = Guid.NewGuid(),
            DestinatarioUsuarioId = Guid.NewGuid(),
            Tipo = TipoNotificacao.AgendamentoCriado,
            Prioridade = PrioridadeNotificacao.Normal,
            Titulo = "Novo agendamento",
            Corpo = "Você tem um novo agendamento.",
            Payload = "{}"
        };

        if (jaLida)
            n.MarcarComoLida();

        return n;
    }

    [Fact]
    public void MarcarComoLida_NaoLida_DefineLidaEm()
    {
        var n = CriarNotificacao();
        var antes = DateTime.UtcNow;

        n.MarcarComoLida();

        Assert.NotNull(n.LidaEm);
        Assert.True(n.LidaEm >= antes);
    }

    [Fact]
    public void MarcarComoLida_NaoLida_AtualizaUpdatedAt()
    {
        var n = CriarNotificacao();
        n.MarcarComoLida();
        Assert.NotNull(n.UpdatedAt);
    }

    [Fact]
    public void MarcarComoLida_JaLida_NaoAlteraDataDeLeitura()
    {
        var n = CriarNotificacao(jaLida: true);
        var dataOriginal = n.LidaEm;

        n.MarcarComoLida();

        Assert.Equal(dataOriginal, n.LidaEm);
    }

    [Fact]
    public void MarcarComoLida_JaLida_Idempotente()
    {
        var n = CriarNotificacao();
        n.MarcarComoLida();
        var dataOriginal = n.LidaEm;

        n.MarcarComoLida();
        n.MarcarComoLida();

        Assert.Equal(dataOriginal, n.LidaEm);
    }

    [Fact]
    public void MarcarComoLida_NovaNotificacao_LidaEhNull()
    {
        var n = CriarNotificacao();
        Assert.Null(n.LidaEm);
    }
}
