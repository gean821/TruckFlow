using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;

namespace TruckFlow.Test.Domain;

public class AgendamentoTests
{
    [Fact]
    public void AlterarStatus_TransicaoValida_AtualizaStatusEUpdatedAt()
    {
        var agendamento = CriarAgendamentoDisponivel();

        agendamento.AlterarStatus(StatusAgendamento.Agendado);

        Assert.Equal(StatusAgendamento.Agendado, agendamento.StatusAgendamento);
        Assert.NotNull(agendamento.UpdatedAt);
    }

    [Fact]
    public void AlterarStatus_TransicaoInvalida_LancaException()
    {
        var agendamento = CriarAgendamentoDisponivel();
        agendamento.AlterarStatus(StatusAgendamento.Agendado);
        agendamento.AlterarStatus(StatusAgendamento.EmAndamento);
        agendamento.AlterarStatus(StatusAgendamento.Finalizado);

        var ex = Assert.Throws<Exception>(() =>
            agendamento.AlterarStatus(StatusAgendamento.Cancelado));

        Assert.Contains("Transição inválida", ex.Message);
    }

    [Fact]
    public void Reservar_VagaDisponivel_AlteraStatusEAtribuiDados()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId);
        var usuarioId = Guid.NewGuid();

        agendamento.Reservar(usuarioId, notaFiscal, TipoVeiculo.CarretaDoisEixos, "ABC-1234");

        Assert.Equal(StatusAgendamento.Agendado, agendamento.StatusAgendamento);
        Assert.Equal(usuarioId, agendamento.UsuarioId);
        Assert.Equal(notaFiscal.Id, agendamento.NotaFiscalId);
        Assert.Equal(notaFiscal.PesoBruto, agendamento.VolumeCarga);
        Assert.Equal(TipoVeiculo.CarretaDoisEixos, agendamento.TipoVeiculo);
    }

    [Fact]
    public void Reservar_UsaPlacaDaNotaFiscalQuandoInformada()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId, placaVeiculo: "XYZ-9999");

        agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, "ABC-1234");

        Assert.Equal("XYZ-9999", agendamento.PlacaVeiculo);
    }

    [Fact]
    public void Reservar_UsaPlacaInformadaQuandoNotaNaoTemPlaca()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId, placaVeiculo: null);

        agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, "ABC-1234");

        Assert.Equal("ABC-1234", agendamento.PlacaVeiculo);
    }

    [Fact]
    public void Reservar_VagaJaAgendada_LancaException()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null);

        var ex = Assert.Throws<Exception>(() =>
            agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null));

        Assert.Contains("não está disponível", ex.Message);
    }

    [Fact]
    public void Reservar_VagaExpirada_LancaException()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(
            produtoId: produtoId,
            dataFim: DateTime.UtcNow.AddHours(-1));
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId);

        var ex = Assert.Throws<Exception>(() =>
            agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null));

        Assert.Contains("já expirou", ex.Message);
    }

    [Fact]
    public void Reservar_SemProdutoNaVaga_LancaException()
    {
        var agendamento = CriarAgendamentoDisponivel(produtoId: null);
        var notaFiscal = CriarNotaFiscal(produtoId: Guid.NewGuid());

        var ex = Assert.Throws<Exception>(() =>
            agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null));

        Assert.Contains("Vaga sem produto definido", ex.Message);
    }

    [Fact]
    public void Reservar_NotaSemProdutoDaVaga_LancaException()
    {
        var agendamento = CriarAgendamentoDisponivel(produtoId: Guid.NewGuid());
        var notaFiscal = CriarNotaFiscal(produtoId: Guid.NewGuid());

        var ex = Assert.Throws<Exception>(() =>
            agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null));

        Assert.Contains("nota fiscal não contém o produto", ex.Message);
    }

    [Fact]
    public void Reservar_FornecedorDiferente_LancaException()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(
            produtoId: produtoId,
            fornecedorId: Guid.NewGuid());
        var notaFiscal = CriarNotaFiscal(
            produtoId: produtoId,
            fornecedorId: Guid.NewGuid());

        var ex = Assert.Throws<Exception>(() =>
            agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null));

        Assert.Contains("exclusiva de outro fornecedor", ex.Message);
    }

    [Fact]
    public void Reservar_SemFornecedorNaVaga_AceitaQualquerFornecedor()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId, fornecedorId: null);
        var notaFiscal = CriarNotaFiscal(produtoId: produtoId);

        agendamento.Reservar(Guid.NewGuid(), notaFiscal, null, null);

        Assert.Equal(StatusAgendamento.Agendado, agendamento.StatusAgendamento);
    }

    [Fact]
    public void RegistrarChegada_AgendamentoAgendado_MudaParaEmAndamento()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);

        agendamento.RegistrarChegada();

        Assert.Equal(StatusAgendamento.EmAndamento, agendamento.StatusAgendamento);
    }

    [Fact]
    public void RegistrarChegada_ComUsuario_AdicionaDomainEvent()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);

        agendamento.RegistrarChegada();

        Assert.Single(agendamento.DomainEvents);
        Assert.IsType<AgendamentoEvent.MotoristaChegouEvent>(agendamento.DomainEvents.First());
    }

    [Fact]
    public void Cancelar_AgendamentoAgendado_MudaParaCancelado()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);

        agendamento.Cancelar("motivo teste");

        Assert.Equal(StatusAgendamento.Cancelado, agendamento.StatusAgendamento);
    }

    [Fact]
    public void Cancelar_SempreAdicionaDomainEvent()
    {
        var agendamento = CriarAgendamentoDisponivel();

        agendamento.Cancelar("motivo");

        Assert.Single(agendamento.DomainEvents);
        Assert.IsType<AgendamentoCanceladoEvent>(agendamento.DomainEvents.First());
    }

    [Fact]
    public void Cancelar_AgendamentoFinalizado_LancaException()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);
        agendamento.RegistrarChegada();
        agendamento.FinalizarOperacao();

        Assert.Throws<Exception>(() => agendamento.Cancelar());
    }

    [Fact]
    public void FinalizarOperacao_EmAndamento_MudaParaFinalizado()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);
        agendamento.RegistrarChegada();

        agendamento.FinalizarOperacao();

        Assert.Equal(StatusAgendamento.Finalizado, agendamento.StatusAgendamento);
    }

    [Fact]
    public void Reagendar_NovasDatasValidas_AtualizaDatas()
    {
        var agendamento = CriarAgendamentoDisponivel();
        var novoInicio = DateTime.UtcNow.AddDays(2);
        var novoFim = DateTime.UtcNow.AddDays(2).AddHours(2);

        agendamento.Reagendar(novoInicio, novoFim);

        Assert.Equal(novoInicio, agendamento.DataInicio);
        Assert.Equal(novoFim, agendamento.DataFim);
        Assert.NotNull(agendamento.UpdatedAt);
    }

    [Fact]
    public void Reagendar_MesmasDatas_NaoAlteraNemAdicionaEvent()
    {
        var inicio = DateTime.UtcNow.AddHours(1);
        var fim = DateTime.UtcNow.AddHours(3);
        var agendamento = CriarAgendamentoDisponivel(dataInicio: inicio, dataFim: fim);

        agendamento.Reagendar(inicio, fim);

        Assert.Empty(agendamento.DomainEvents);
        Assert.Null(agendamento.UpdatedAt);
    }

    [Fact]
    public void Reagendar_ComMotoristaAgendado_AdicionaDomainEvent()
    {
        var produtoId = Guid.NewGuid();
        var agendamento = CriarAgendamentoDisponivel(produtoId: produtoId);
        agendamento.Reservar(Guid.NewGuid(), CriarNotaFiscal(produtoId: produtoId), null, null);
        agendamento.ClearDomainEvents();

        agendamento.Reagendar(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(2));

        Assert.Single(agendamento.DomainEvents);
        Assert.IsType<AgendamentoEvent.AgendamentoReagendadoEvent>(
            agendamento.DomainEvents.First());
    }

    [Fact]
    public void Reagendar_SemMotoristaVinculado_NaoAdicionaDomainEvent()
    {
        var agendamento = CriarAgendamentoDisponivel();

        agendamento.Reagendar(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(2));

        Assert.Empty(agendamento.DomainEvents);
    }

    [Theory]
    [InlineData(StatusAgendamento.Disponivel,  true)]
    [InlineData(StatusAgendamento.Pendente,    true)]
    [InlineData(StatusAgendamento.Agendado,    true)]
    [InlineData(StatusAgendamento.EmAndamento, false)]
    [InlineData(StatusAgendamento.Finalizado,  false)]
    [InlineData(StatusAgendamento.Cancelado,   false)]
    [InlineData(StatusAgendamento.Expirado,    false)]
    public void PodeExpirarNaData_DataJaPassada_RetornaConformeStatus(
        StatusAgendamento status, bool esperado)
    {
        var agendamento = new Agendamento
        {
            TipoCarga = TipoCarga.Milho,
            DataFim = DateTime.UtcNow.AddHours(-1),
            StatusAgendamento = status,
            EmpresaId = Guid.NewGuid()
        };

        Assert.Equal(esperado, agendamento.PodeExpirarNaData(DateTime.UtcNow));
    }

    [Fact]
    public void PodeExpirarNaData_DataAindaNaoExpirou_RetornaFalse()
    {
        var agendamento = new Agendamento
        {
            TipoCarga = TipoCarga.Milho,
            DataFim = DateTime.UtcNow.AddHours(2),
            StatusAgendamento = StatusAgendamento.Disponivel,
            EmpresaId = Guid.NewGuid()
        };

        Assert.False(agendamento.PodeExpirarNaData(DateTime.UtcNow));
    }

    [Fact]
    public void Expirar_VagaDisponivel_MudaParaExpirado()
    {
        var agendamento = CriarAgendamentoDisponivel();

        agendamento.Expirar();

        Assert.Equal(StatusAgendamento.Expirado, agendamento.StatusAgendamento);
    }

    [Fact]
    public void ClearDomainEvents_LimpaTodosOsEventos()
    {
        var agendamento = CriarAgendamentoDisponivel();
        agendamento.Cancelar();

        agendamento.ClearDomainEvents();

        Assert.Empty(agendamento.DomainEvents);
    }

    private static Agendamento CriarAgendamentoDisponivel(
        Guid? produtoId = null,
        Guid? fornecedorId = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null) =>
        new()
        {
            TipoCarga = TipoCarga.Milho,
            DataInicio = dataInicio ?? DateTime.UtcNow.AddHours(1),
            DataFim = dataFim ?? DateTime.UtcNow.AddHours(3),
            StatusAgendamento = StatusAgendamento.Disponivel,
            EmpresaId = Guid.NewGuid(),
            ProdutoId = produtoId,
            FornecedorId = fornecedorId
        };

    private static NotaFiscal CriarNotaFiscal(
        Guid produtoId,
        Guid? fornecedorId = null,
        string? placaVeiculo = null)
    {
        var nf = new NotaFiscal
        {
            Id = Guid.NewGuid(),
            ChaveAcesso = new string('1', 44),
            Numero = 1001,
            Serie = "001",
            DataEmissao = DateTime.UtcNow,
            EmitenteNome = "Fornecedor Teste Ltda",
            EmitenteCnpj = "12345678000190",
            DestinatarioNome = "Aurora Alimentos",
            DestinatarioCpfCnpj = "98765432000101",
            ValorTotal = 1000m,
            PesoBruto = 500m,
            TipoCarga = TipoCarga.Milho,
            FornecedorId = fornecedorId ?? Guid.NewGuid(),
            EmpresaId = Guid.NewGuid(),
            UploadedByUserId = Guid.NewGuid(),
            PlacaVeiculo = placaVeiculo
        };

        nf.Itens = new List<NotaFiscalItem>
        {
            new()
            {
                NotaFiscal = nf,
                Codigo = "PROD-001",
                Descricao = "Produto de Teste",
                ValorUnitario = 100m,
                ValorTotal = 1000m,
                ProdutoId = produtoId
            }
        };

        return nf;
    }
}
