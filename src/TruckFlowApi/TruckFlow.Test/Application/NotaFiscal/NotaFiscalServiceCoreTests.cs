using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TruckFlow.Application;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Sefaz;
using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Application.NotaFiscal.Fakes;

namespace TruckFlow.Test.Application.NotaFiscal;

public class NotaFiscalServiceCoreTests
{
    private const string CnpjAurora = "99888777000166";
    private const string CnpjFornecedor = "11222333000181";
    private const string ChaveValida = "35240112345678000199550010000012345123456789";

    private sealed class BuiltService
    {
        public required NotaFiscalService Service { get; init; }
        public required InMemoryEmpresaRepositorio EmpresaRepo { get; init; }
        public required InMemoryFornecedorRepositorio FornecedorRepo { get; init; }
        public required InMemoryProdutoRepositorio ProdutoRepo { get; init; }
        public required InMemoryNotaFiscalRepositorio NotaFiscalRepo { get; init; }
    }

    private static BuiltService Criar(ISefazClient? sefazClient = null)
    {
        var empresaRepo = new InMemoryEmpresaRepositorio();
        var fornecedorRepo = new InMemoryFornecedorRepositorio();
        var produtoRepo = new InMemoryProdutoRepositorio();
        var produtoFornecedorRepo = new InMemoryProdutoFornecedorRepositorio();
        var notaFiscalRepo = new InMemoryNotaFiscalRepositorio();
        var learningService = new ProdutoLearningService(produtoRepo);
        var empresaContext = new NoOpEmpresaContext();

        var service = new NotaFiscalService(
            notaFiscalRepo,
            fornecedorRepo,
            produtoFornecedorRepo,
            new NotaFiscalParsedDtoValidator(),
            new NotaFiscalItemDtoValidator(),
            learningService,
            produtoRepo,
            NullLogger<NotaFiscalService>.Instance,
            empresaRepo,
            empresaContext,
            sefazClient ?? new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())),
            Options.Create(new SefazOptions()));

        empresaRepo.Empresas.Add(new Empresa
        {
            RazaoSocial = "Aurora Alimentos Teste",
            NomeFantasia = "Aurora",
            Cnpj = CnpjAurora,
            Email = "teste@aurora.com",
            Logradouro = "Rodovia Teste",
            Numero = "500",
            Bairro = "Distrito Industrial",
            Cidade = "Mandaguari",
            Estado = "PR",
            Cep = "86660000"
        });

        return new BuiltService
        {
            Service = service,
            EmpresaRepo = empresaRepo,
            FornecedorRepo = fornecedorRepo,
            ProdutoRepo = produtoRepo,
            NotaFiscalRepo = notaFiscalRepo
        };
    }

    private static NotaFiscalParsedDto DtoValido(string chave = ChaveValida) => new()
    {
        ChaveAcesso = chave,
        Numero = 12345,
        Fornecedor = "Fornecedor Teste LTDA",
        Serie = "1",
        DataEmissao = DateTime.UtcNow.AddDays(-1),
        EmitenteNome = "Fornecedor Teste LTDA",
        EmitenteCnpj = CnpjFornecedor,
        DestinatarioNome = "Aurora Alimentos Teste",
        DestinatarioCpfCnpj = CnpjAurora,
        ValorTotal = 1500.00m,
        PesoBruto = 1000.000m,
        VolumeQuantidade = 20,
        PlacaVeiculo = "ABC1D23",
        TipoCarga = TipoCarga.Milho,
        Itens =
        [
            new NotaFiscalItemDto
            {
                Codigo = "PROD001",
                Ean = "7891000100103",
                Descricao = "Milho em Grao",
                Quantidade = 1000m,
                Unidade = "KG",
                ValorUnitario = 1.5m,
                ValorTotal = 1500.00m
            }
        ]
    };

    // ---------- SaveParsedNotaAsync ----------

    [Fact]
    public async Task SaveParsedNotaAsync_DtoComValorTotalZerado_LancaValidationException()
    {
        var built = Criar();
        var dto = DtoValido();
        dto.ValorTotal = 0;

        await Assert.ThrowsAsync<ValidationException>(
            () => built.Service.SaveParsedNotaAsync(dto, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SaveParsedNotaAsync_ItemComValorTotalInconsistente_LancaValidationException()
    {
        var built = Criar();
        var dto = DtoValido();
        dto.Itens = [new NotaFiscalItemDto
        {
            Codigo = "PROD001",
            Descricao = "Milho em Grao",
            Quantidade = 10,
            Unidade = "KG",
            ValorUnitario = 1.5m,
            ValorTotal = 999m // != Quantidade * ValorUnitario
        }];

        await Assert.ThrowsAsync<ValidationException>(
            () => built.Service.SaveParsedNotaAsync(dto, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SaveParsedNotaAsync_EmpresaDestinatariaNaoCadastrada_LancaBusinessException()
    {
        var built = Criar();
        built.EmpresaRepo.Empresas.Clear();
        var dto = DtoValido();

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => built.Service.SaveParsedNotaAsync(dto, Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("Empresa destinatária não cadastrada", ex.Message);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_FornecedorNovo_CriaFornecedorECria_Nota()
    {
        var built = Criar();
        var dto = DtoValido();

        var resultado = await built.Service.SaveParsedNotaAsync(dto, Guid.NewGuid(), CancellationToken.None);

        Assert.Single(built.FornecedorRepo.Fornecedores);
        Assert.Equal(CnpjFornecedor, built.FornecedorRepo.Fornecedores[0].Cnpj);
        Assert.Equal(ChaveValida, resultado.ChaveAcesso);
        Assert.NotEqual(Guid.Empty, resultado.FornecedorId);
        Assert.Single(built.NotaFiscalRepo.Notas);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_FornecedorJaExistente_ReaproveitaSemDuplicar()
    {
        var built = Criar();
        built.FornecedorRepo.Fornecedores.Add(new Fornecedor
        {
            Nome = "Fornecedor Teste LTDA",
            Cnpj = CnpjFornecedor,
            EmpresaId = built.EmpresaRepo.Empresas[0].Id,
            Empresa = built.EmpresaRepo.Empresas[0]
        });

        await built.Service.SaveParsedNotaAsync(DtoValido(), Guid.NewGuid(), CancellationToken.None);

        Assert.Single(built.FornecedorRepo.Fornecedores);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_NotaJaExisteMesmaEmpresaSemAgendamento_ReaproveitaIdempotente()
    {
        var built = Criar();
        var empresaId = built.EmpresaRepo.Empresas[0].Id;
        var fornecedor = new Fornecedor { Nome = "Fornecedor Teste LTDA", Cnpj = CnpjFornecedor, EmpresaId = empresaId, Empresa = built.EmpresaRepo.Empresas[0] };
        built.FornecedorRepo.Fornecedores.Add(fornecedor);

        var notaExistente = new TruckFlow.Domain.Entities.NotaFiscal
        {
            ChaveAcesso = ChaveValida,
            Numero = 12345,
            Serie = "1",
            DataEmissao = DateTime.UtcNow.AddDays(-1),
            EmitenteNome = "Fornecedor Teste LTDA",
            EmitenteCnpj = CnpjFornecedor,
            DestinatarioNome = "Aurora Alimentos Teste",
            DestinatarioCpfCnpj = CnpjAurora,
            ValorTotal = 1500.00m,
            PesoBruto = 1000.000m,
            PlacaVeiculo = "ABC1D23",
            TipoCarga = TipoCarga.Milho,
            EmpresaId = empresaId,
            FornecedorId = fornecedor.Id,
            Itens = []
        };
        built.NotaFiscalRepo.Notas.Add(notaExistente);

        var resultado = await built.Service.SaveParsedNotaAsync(DtoValido(), Guid.NewGuid(), CancellationToken.None);

        Assert.Single(built.NotaFiscalRepo.Notas); // não duplicou
        Assert.Equal(ChaveValida, resultado.ChaveAcesso);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_NotaJaExisteComAgendamentoAtivo_LancaBusinessException()
    {
        var built = Criar();
        var empresaId = built.EmpresaRepo.Empresas[0].Id;

        var notaExistente = new TruckFlow.Domain.Entities.NotaFiscal
        {
            ChaveAcesso = ChaveValida,
            Numero = 12345,
            Serie = "1",
            DataEmissao = DateTime.UtcNow.AddDays(-1),
            EmitenteNome = "Fornecedor Teste LTDA",
            EmitenteCnpj = CnpjFornecedor,
            DestinatarioNome = "Aurora Alimentos Teste",
            DestinatarioCpfCnpj = CnpjAurora,
            ValorTotal = 1500.00m,
            PesoBruto = 1000.000m,
            TipoCarga = TipoCarga.Milho,
            EmpresaId = empresaId,
            AgendamentoId = Guid.NewGuid(),
            Agendamento = new Agendamento
            {
                TipoCarga = TipoCarga.Milho,
                StatusAgendamento = StatusAgendamento.Agendado,
                EmpresaId = empresaId
            },
            Itens = []
        };
        built.NotaFiscalRepo.Notas.Add(notaExistente);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => built.Service.SaveParsedNotaAsync(DtoValido(), Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("agendamento ativo", ex.Message);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_NotaJaExisteEmOutraEmpresa_LancaBusinessException()
    {
        var built = Criar();

        var notaDeOutraEmpresa = new TruckFlow.Domain.Entities.NotaFiscal
        {
            ChaveAcesso = ChaveValida,
            Numero = 12345,
            Serie = "1",
            DataEmissao = DateTime.UtcNow.AddDays(-1),
            EmitenteNome = "Fornecedor Teste LTDA",
            EmitenteCnpj = CnpjFornecedor,
            DestinatarioNome = "Outra Empresa",
            DestinatarioCpfCnpj = "00000000000191",
            ValorTotal = 1500.00m,
            PesoBruto = 1000.000m,
            TipoCarga = TipoCarga.Milho,
            EmpresaId = Guid.NewGuid(), // empresa diferente da Aurora
            Itens = []
        };
        built.NotaFiscalRepo.Notas.Add(notaDeOutraEmpresa);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => built.Service.SaveParsedNotaAsync(DtoValido(), Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("já está cadastrada em outra empresa", ex.Message);
    }

    [Fact]
    public async Task SaveParsedNotaAsync_ProdutoSistemaIdForjadoNoDto_EhIgnoradoERecomputadoServerSide()
    {
        var built = Criar();
        var dto = DtoValido();
        dto.Itens.First().ProdutoSistemaId = Guid.NewGuid(); // produto que não existe no catálogo
        dto.Itens.First().Status = NotaFiscalItemStatus.Matched; // forjado

        var resultado = await built.Service.SaveParsedNotaAsync(dto, Guid.NewGuid(), CancellationToken.None);

        var item = resultado.Itens.Single();
        Assert.Equal(NotaFiscalItemStatus.PendenteRevisao, item.Status);
        Assert.Null(item.ProdutoSistemaId);
    }

    // ---------- ObterPorChaveAsync ----------

    [Fact]
    public async Task ObterPorChaveAsync_NotaNaoExiste_RetornaNull()
    {
        var built = Criar();

        var resultado = await built.Service.ObterPorChaveAsync(ChaveValida, CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorChaveAsync_SemTenantNoContexto_RetornaNotaIndependenteDaEmpresa()
    {
        var built = Criar();
        var empresaId = built.EmpresaRepo.Empresas[0].Id;
        built.NotaFiscalRepo.Notas.Add(NotaFiscalMinima(empresaId));

        var resultado = await built.Service.ObterPorChaveAsync(ChaveValida, CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal(ChaveValida, resultado!.ChaveAcesso);
    }

    [Fact]
    public async Task ObterPorChaveAsync_TenantDiferenteDaNota_RetornaNullIsolamentoMultiTenant()
    {
        var empresaRepo = new InMemoryEmpresaRepositorio();
        var fornecedorRepo = new InMemoryFornecedorRepositorio();
        var produtoRepo = new InMemoryProdutoRepositorio();
        var notaFiscalRepo = new InMemoryNotaFiscalRepositorio();
        var empresaContext = new NoOpEmpresaContext { EmpresaIdOrNull = Guid.NewGuid() }; // outro tenant

        var service = new NotaFiscalService(
            notaFiscalRepo, fornecedorRepo, new InMemoryProdutoFornecedorRepositorio(),
            new NotaFiscalParsedDtoValidator(), new NotaFiscalItemDtoValidator(),
            new ProdutoLearningService(produtoRepo), produtoRepo,
            NullLogger<NotaFiscalService>.Instance, empresaRepo, empresaContext,
            new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())),
            Options.Create(new SefazOptions()));

        notaFiscalRepo.Notas.Add(NotaFiscalMinima(Guid.NewGuid())); // nota é de outra empresa

        var resultado = await service.ObterPorChaveAsync(ChaveValida, CancellationToken.None);

        Assert.Null(resultado);
    }

    // ---------- ValidarNaSefazAsync ----------

    [Fact]
    public async Task ValidarNaSefazAsync_ChaveComTamanhoInvalido_LancaBusinessException()
    {
        var built = Criar();

        await Assert.ThrowsAsync<BusinessException>(
            () => built.Service.ValidarNaSefazAsync("123", CancellationToken.None));
    }

    [Fact]
    public async Task ValidarNaSefazAsync_NotaNaoPersistida_RetornaResultadoSemAtualizarBanco()
    {
        var built = Criar();
        const string chaveAutorizadaFake = "35240199999999999999550010000099999999999999";

        var resultado = await built.Service.ValidarNaSefazAsync(chaveAutorizadaFake, CancellationToken.None);

        Assert.True(resultado.Autorizada);
        Assert.False(resultado.NotaPersistidaAtualizada);
        Assert.Empty(built.NotaFiscalRepo.Notas);
    }

    [Fact]
    public async Task ValidarNaSefazAsync_NotaPersistidaEAutorizada_AtualizaStatusParaValidada()
    {
        var built = Criar();
        var empresaId = built.EmpresaRepo.Empresas[0].Id;
        const string chaveAutorizadaFake = "35240199999999999999550010000099999999999999";
        var nota = NotaFiscalMinima(empresaId, chaveAutorizadaFake);
        built.NotaFiscalRepo.Notas.Add(nota);

        var resultado = await built.Service.ValidarNaSefazAsync(chaveAutorizadaFake, CancellationToken.None);

        Assert.True(resultado.NotaPersistidaAtualizada);
        Assert.Equal(NotaFiscalStatus.Validada, nota.Status);
        Assert.Equal(100, nota.StatusSefaz);
        Assert.NotNull(nota.UltimaValidacaoSefaz);
        Assert.Equal(FonteValidacao.ConsultaProtocolo, nota.FonteValidacao);
    }

    [Fact]
    public async Task ValidarNaSefazAsync_NotaPersistidaECancelada_AtualizaStatusParaRejeitada()
    {
        var built = Criar();
        var empresaId = built.EmpresaRepo.Empresas[0].Id;
        const string chaveCancelada = "35240100000000000000000010000000000000000001"; // sufixo 0001 -> cancelada no fake
        var nota = NotaFiscalMinima(empresaId, chaveCancelada);
        built.NotaFiscalRepo.Notas.Add(nota);

        await built.Service.ValidarNaSefazAsync(chaveCancelada, CancellationToken.None);

        Assert.Equal(NotaFiscalStatus.Rejeitada, nota.Status);
    }

    [Fact]
    public async Task ValidarNaSefazAsync_UsaUfExtraidaDaChave_QuandoValida()
    {
        var spy = new UfCapturingSefazClient();
        var built = Criar(spy);
        const string chaveParana = "41240199999999999999550010000099999999999999"; // cUF=41 -> PR

        await built.Service.ValidarNaSefazAsync(chaveParana, CancellationToken.None);

        Assert.Equal("PR", spy.UltimaUfRecebida);
    }

    private static TruckFlow.Domain.Entities.NotaFiscal NotaFiscalMinima(Guid empresaId, string chave = ChaveValida) => new()
    {
        ChaveAcesso = chave,
        Numero = 1,
        Serie = "1",
        DataEmissao = DateTime.UtcNow.AddDays(-1),
        EmitenteNome = "Fornecedor Teste LTDA",
        EmitenteCnpj = CnpjFornecedor,
        DestinatarioNome = "Aurora Alimentos Teste",
        DestinatarioCpfCnpj = CnpjAurora,
        ValorTotal = 1500.00m,
        PesoBruto = 1000.000m,
        TipoCarga = TipoCarga.Milho,
        EmpresaId = empresaId,
        Itens = []
    };

    private sealed class UfCapturingSefazClient : ISefazClient
    {
        public string? UltimaUfRecebida { get; private set; }
        private readonly FakeSefazClient _inner = new(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions()));

        public Task<ConsultaProtocoloResultado> ConsultarProtocoloAsync(string chaveAcesso, string ufEmitente, CancellationToken token)
        {
            UltimaUfRecebida = ufEmitente;
            return _inner.ConsultarProtocoloAsync(chaveAcesso, ufEmitente, token);
        }

        public Task<ConsultaDistribuicaoResultado> ConsultarDistribuicaoAsync(string chaveAcesso, CancellationToken token) =>
            _inner.ConsultarDistribuicaoAsync(chaveAcesso, token);
    }
}
