using BenchmarkDotNet.Attributes;
using TruckFlow.Application.Sefaz;
using TruckFlow.Application.Validators.EanValidators;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Rules;

namespace TruckFlow.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class DominioBenchmarks
{
    private Agendamento _agendamentoDisponivel = null!;
    private NotaFiscal _notaFiscal = null!;
    private readonly Guid _produtoId = Guid.NewGuid();

    [GlobalSetup]
    public void Setup()
    {
        _notaFiscal = new NotaFiscal
        {
            Id = Guid.NewGuid(),
            ChaveAcesso = new string('1', 44),
            Numero = 1001,
            Serie = "001",
            DataEmissao = DateTime.UtcNow.AddDays(-1),
            EmitenteNome = "Fornecedor Teste Ltda",
            EmitenteCnpj = "12345678000190",
            DestinatarioNome = "Aurora Alimentos",
            DestinatarioCpfCnpj = "98765432000101",
            ValorTotal = 1000m,
            PesoBruto = 500m,
            TipoCarga = TipoCarga.Milho,
            FornecedorId = Guid.NewGuid(),
            EmpresaId = Guid.NewGuid(),
            UploadedByUserId = Guid.NewGuid()
        };
        _notaFiscal.Itens = new List<NotaFiscalItem>
        {
            new()
            {
                NotaFiscal = _notaFiscal,
                Codigo = "PROD-001",
                Descricao = "Produto de Teste",
                ValorUnitario = 100m,
                ValorTotal = 1000m,
                ProdutoId = _produtoId
            }
        };

        _agendamentoDisponivel = new Agendamento
        {
            TipoCarga = TipoCarga.Milho,
            DataInicio = DateTime.UtcNow.AddHours(1),
            DataFim = DateTime.UtcNow.AddHours(3),
            StatusAgendamento = StatusAgendamento.Disponivel,
            EmpresaId = Guid.NewGuid(),
            ProdutoId = _produtoId
        };
    }

    [IterationSetup]
    public void ResetAgendamento()
    {
        _agendamentoDisponivel.StatusAgendamento = StatusAgendamento.Disponivel;
        _agendamentoDisponivel.UsuarioId = null;
        _agendamentoDisponivel.NotaFiscalId = null;
        _agendamentoDisponivel.UpdatedAt = null;
        _agendamentoDisponivel.ClearDomainEvents();
    }

    [Benchmark]
    public void Agendamento_Reservar()
    {
        _agendamentoDisponivel.Reservar(Guid.NewGuid(), _notaFiscal, TipoVeiculo.CarretaDoisEixos, null);
    }

    [Benchmark]
    public bool StatusAgendamento_PodeTransitar_Valido()
    {
        return StatusAgendamento.Disponivel.PodeTransitarPara(StatusAgendamento.Agendado);
    }

    [Benchmark]
    public bool StatusAgendamento_PodeTransitar_Invalido()
    {
        return StatusAgendamento.Finalizado.PodeTransitarPara(StatusAgendamento.Cancelado);
    }

    [Benchmark]
    public bool EanValidator_Ean13_Valido()
    {
        return EanValidator.IsValid("7891000315507");
    }

    [Benchmark]
    public bool EanValidator_Ean13_Invalido()
    {
        return EanValidator.IsValid("7891000315508");
    }

    [Benchmark]
    public bool EanValidator_Ean8_Valido()
    {
        return EanValidator.IsValid("96385074");
    }

    [Benchmark]
    public string? ChaveAcesso_ExtrairUf_SP()
    {
        return ChaveAcessoHelper.ExtrairUfEmitente("35" + new string('1', 42));
    }

    [Benchmark]
    public string? ChaveAcesso_ExtrairUf_CodigoInexistente()
    {
        return ChaveAcessoHelper.ExtrairUfEmitente("99" + new string('1', 42));
    }

    [Benchmark]
    public bool Agendamento_PodeExpirar_Disponivel()
    {
        return _agendamentoDisponivel.PodeExpirarNaData(DateTime.UtcNow.AddHours(4));
    }
}
