using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Integration.Fixtures;

internal static class TestDataSeeder
{
    public static Empresa NovaEmpresa(string nome) => new()
    {
        RazaoSocial = nome,
        NomeFantasia = nome,
        Cnpj = NovoCnpj(),
        Email = $"{Guid.NewGuid():N}@empresa.teste",
        Logradouro = "Rua Teste",
        Numero = "100",
        Bairro = "Centro",
        Cidade = "Curitiba",
        Estado = "PR",
        Cep = "80000-000",
        Ativa = true,
    };

    public static UnidadeEntrega NovaUnidadeEntrega(Guid empresaId) => new()
    {
        EmpresaId = empresaId,
        Nome = "Unidade Teste",
        Localizacao = "Pátio 1",
        Ativa = true,
    };

    public static Fornecedor NovoFornecedor(Guid empresaId) => new()
    {
        EmpresaId = empresaId,
        Nome = "Fornecedor Teste",
        Cnpj = NovoCnpj(),
    };

    public static Produto NovoProduto(Guid empresaId, Guid localDescargaId, LocalDescarga localDescarga) => new()
    {
        EmpresaId = empresaId,
        Nome = "Soja",
        LocalDescargaId = localDescargaId,
        LocalDescarga = localDescarga,
    };

    public static LocalDescarga NovoLocalDescarga(Guid empresaId, Guid unidadeEntregaId, UnidadeEntrega unidadeEntrega) => new()
    {
        EmpresaId = empresaId,
        Nome = "Doca 1",
        UnidadeEntregaId = unidadeEntregaId,
        UnidadeEntrega = unidadeEntrega,
        Ativa = true,
    };

    public static Usuario NovoUsuarioMotorista(Guid? empresaId = null)
    {
        var id = Guid.NewGuid();
        return new Usuario
        {
            Id = id,
            UserName = $"motorista-{id:N}",
            NormalizedUserName = $"MOTORISTA-{id:N}".ToUpperInvariant(),
            Email = $"motorista-{id:N}@teste.com",
            NormalizedEmail = $"MOTORISTA-{id:N}@TESTE.COM".ToUpperInvariant(),
            EmailConfirmed = true,
            PasswordHash = "fake-hash",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            EmpresaId = empresaId,
        };
    }

    public static NotaFiscal NovaNotaFiscal(
        Guid empresaId,
        Guid fornecedorId,
        Guid produtoId,
        decimal? pesoBruto = 0m)
    {
        var nota = new NotaFiscal
        {
            EmpresaId = empresaId,
            FornecedorId = fornecedorId,
            ChaveAcesso = NovaChaveAcesso(),
            Numero = Random.Shared.NextInt64(1, 999_999),
            Serie = "1",
            DataEmissao = DateTime.UtcNow,
            EmitenteNome = "Fornecedor Teste",
            EmitenteCnpj = NovoCnpj(),
            DestinatarioNome = "Empresa Teste",
            DestinatarioCpfCnpj = NovoCnpj(),
            ValorTotal = 1000m,
            PesoBruto = pesoBruto,
            TipoCarga = TipoCarga.SojaCrua,
            Status = NotaFiscalStatus.Validada,
            UploadedByUserId = Guid.NewGuid(),
        };

        nota.Itens.Add(new NotaFiscalItem
        {
            EmpresaId = empresaId,
            NotaFiscal = nota,
            Codigo = "ITEM-1",
            Descricao = "Item Teste",
            Quantidade = 1,
            ValorUnitario = 1000m,
            ValorTotal = 1000m,
            ProdutoId = produtoId,
        });

        return nota;
    }

    public static Agendamento NovaVagaDisponivel(
        Guid empresaId,
        Guid unidadeEntregaId,
        Guid produtoId,
        DateTime? dataInicio = null) => new()
    {
        EmpresaId = empresaId,
        UnidadeEntregaId = unidadeEntregaId,
        ProdutoId = produtoId,
        TipoCarga = TipoCarga.SojaCrua,
        StatusAgendamento = StatusAgendamento.Disponivel,
        DataInicio = dataInicio ?? DateTime.UtcNow.AddHours(1),
        DataFim = (dataInicio ?? DateTime.UtcNow.AddHours(1)).AddHours(2),
    };

    private static string NovoCnpj() =>
        Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString();

    private static string NovaChaveAcesso() =>
        Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString("D14") +
        Random.Shared.NextInt64(100_000_000_000_000, 999_999_999_999_999).ToString("D30");
}
