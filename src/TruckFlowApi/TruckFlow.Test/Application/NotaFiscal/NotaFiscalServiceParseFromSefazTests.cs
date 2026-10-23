using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TruckFlow.Application;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Sefaz;
using TruckFlow.Domain.Entities;
using TruckFlow.Test.Application.NotaFiscal.Fakes;

namespace TruckFlow.Test.Application.NotaFiscal;

public class NotaFiscalServiceParseFromSefazTests
{
    private const string ChaveAutorizada = "35240199999999999999550010000099999999999999";

    private static (NotaFiscalService service, InMemoryEmpresaRepositorio empresaRepo, InMemoryFornecedorRepositorio fornecedorRepo, InMemoryProdutoRepositorio produtoRepo)
        CriarService(ISefazClient sefazClient)
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
            parsedValidator: null!,
            itemValidator: null!,
            learningService,
            produtoRepo,
            NullLogger<NotaFiscalService>.Instance,
            empresaRepo,
            empresaContext,
            sefazClient,
            Options.Create(new SefazOptions()));

        return (service, empresaRepo, fornecedorRepo, produtoRepo);
    }

    [Fact]
    public async Task ParseFromSefazAsync_ChaveComTamanhoInvalido_LancaBusinessExceptionSemChamarSefaz()
    {
        var (service, _, _, _) = CriarService(sefazClient: new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())));

        await Assert.ThrowsAsync<BusinessException>(
            () => service.ParseFromSefazAsync("chave-curta", CancellationToken.None));
    }

    [Fact]
    public async Task ParseFromSefazAsync_NotaNaoDisponivelNaSefaz_LancaBusinessException()
    {
        var (service, _, _, _) = CriarService(sefazClient: new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())));

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => service.ParseFromSefazAsync(NotaFiscalXmlFixtures.ChaveNaoDisponivel, CancellationToken.None));

        Assert.Contains("não disponível", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseFromSefazAsync_NotaAutorizada_EmpresaCadastrada_RetornaDtoComDadosDaNota()
    {
        var (service, empresaRepo, _, _) = CriarService(
            sefazClient: new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())));

        empresaRepo.Empresas.Add(new Empresa
        {
            RazaoSocial = "Aurora Alimentos Teste",
            NomeFantasia = "Aurora",
            Cnpj = "99888777000166",
            Email = "teste@aurora.com",
            Logradouro = "Rodovia Teste",
            Numero = "500",
            Bairro = "Distrito Industrial",
            Cidade = "Mandaguari",
            Estado = "PR",
            Cep = "86660000"
        });

        var dto = await service.ParseFromSefazAsync(ChaveAutorizada, CancellationToken.None);

        Assert.Equal("Fornecedor Teste LTDA", dto.EmitenteNome);
        Assert.Equal("Aurora Alimentos Teste", dto.DestinatarioNome);
        Assert.Equal(1500.00m, dto.ValorTotal);
        Assert.Equal(1000.000m, dto.PesoBruto);
        Assert.Equal("ABC1D23", dto.PlacaVeiculo);
        Assert.Single(dto.Itens);
    }

    [Fact]
    public async Task ParseFromSefazAsync_NenhumProdutoCadastrado_ItemFicaPendenteRevisao()
    {
        var (service, empresaRepo, _, _) = CriarService(
            sefazClient: new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())));

        empresaRepo.Empresas.Add(new Empresa
        {
            RazaoSocial = "Aurora Alimentos Teste",
            NomeFantasia = "Aurora",
            Cnpj = "99888777000166",
            Email = "teste@aurora.com",
            Logradouro = "Rodovia Teste",
            Numero = "500",
            Bairro = "Distrito Industrial",
            Cidade = "Mandaguari",
            Estado = "PR",
            Cep = "86660000"
        });

        var dto = await service.ParseFromSefazAsync(ChaveAutorizada, CancellationToken.None);

        var item = Assert.Single(dto.Itens);
        Assert.Equal(TruckFlow.Domain.Enums.NotaFiscalItemStatus.PendenteRevisao, item.Status);
        Assert.Null(item.ProdutoSistemaId);
    }

    [Fact]
    public async Task ParseFromSefazAsync_EmpresaDestinatariaNaoCadastrada_ItensVaoPendentesSemErro()
    {
        var (service, _, _, _) = CriarService(
            sefazClient: new FakeSefazClient(NullLogger<FakeSefazClient>.Instance, Options.Create(new SefazOptions())));

        var dto = await service.ParseFromSefazAsync(ChaveAutorizada, CancellationToken.None);

        var item = Assert.Single(dto.Itens);
        Assert.Equal(TruckFlow.Domain.Enums.NotaFiscalItemStatus.PendenteRevisao, item.Status);
    }
}
