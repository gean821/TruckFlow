using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Test.Application.NotaFiscal.Fakes;
internal sealed class InMemoryEmpresaRepositorio : IEmpresaRepositorio
{
    public List<Empresa> Empresas { get; } = new();

    public Task<Empresa?> GetById(Guid id, CancellationToken token = default) =>
        Task.FromResult(Empresas.FirstOrDefault(e => e.Id == id));

    public Task<Empresa?> GetByCnpj(string cnpj, CancellationToken token = default) =>
        Task.FromResult(Empresas.FirstOrDefault(e => e.Cnpj == cnpj));

    public Task<List<Empresa>> GetAll(CancellationToken token = default) =>
        Task.FromResult(Empresas);

    public Task<Empresa> CreateEmpresa(Empresa local, CancellationToken token = default)
    {
        Empresas.Add(local);
        return Task.FromResult(local);
    }

    public Task<Empresa> Update(Empresa local, CancellationToken token = default) =>
        Task.FromResult(local);

    public Task Delete(Empresa empresa, CancellationToken token = default)
    {
        Empresas.Remove(empresa);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
}

internal sealed class InMemoryFornecedorRepositorio : IFornecedorRepositorio
{
    public List<Fornecedor> Fornecedores { get; } = new();

    public Task<Fornecedor?> GetById(Guid id, CancellationToken token = default) =>
        Task.FromResult(Fornecedores.FirstOrDefault(f => f.Id == id));

    public Task<Fornecedor?> GetByCnpj(string cnpj, CancellationToken token = default) =>
        Task.FromResult(Fornecedores.FirstOrDefault(f => f.Cnpj == cnpj));

    public Task<Fornecedor?> GetByNome(string nome, CancellationToken token = default) =>
        Task.FromResult(Fornecedores.FirstOrDefault(f => f.Nome == nome));

    public Task<List<Fornecedor>> GetAll(CancellationToken token = default) =>
        Task.FromResult(Fornecedores);

    public Task<Fornecedor> CreateFornecedor(Fornecedor fornecedor, CancellationToken token = default)
    {
        Fornecedores.Add(fornecedor);
        return Task.FromResult(fornecedor);
    }

    public Task<Fornecedor> Update(Fornecedor fornecedor, CancellationToken token = default) =>
        Task.FromResult(fornecedor);

    public Task<Fornecedor?> GetByIdWithProdutosAsync(Guid id, CancellationToken token = default) =>
        Task.FromResult(Fornecedores.FirstOrDefault(f => f.Id == id));

    public Task Delete(Fornecedor fornecedor, CancellationToken token = default)
    {
        Fornecedores.Remove(fornecedor);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
}

internal sealed class InMemoryProdutoRepositorio : IProdutoRepositorio
{
    public List<Produto> Produtos { get; } = new();

    public Task<List<Produto>> GetAll(CancellationToken cancellationToken = default) =>
        Task.FromResult(Produtos);

    public Task<Produto?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id));

    public Task<Produto> CreateProduto(Produto produto, CancellationToken cancellationToken = default)
    {
        Produtos.Add(produto);
        return Task.FromResult(produto);
    }

    public Task<Produto> UpdateProduto(Produto produto, CancellationToken cancellationToken = default) =>
        Task.FromResult(produto);

    public Task DeleteProduto(Produto produto, CancellationToken cancellationToken = default)
    {
        Produtos.Remove(produto);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellation = default) => Task.CompletedTask;

    public Task<List<Produto>> GetByIdsAsync(IEnumerable<Guid> produtoIds, CancellationToken token = default) =>
        Task.FromResult(Produtos.Where(p => produtoIds.Contains(p.Id)).ToList());
}

internal sealed class InMemoryProdutoFornecedorRepositorio : IProdutoFornecedorRepositorio
{
    public List<ProdutoFornecedor> Mapeamentos { get; } = new();

    public Task<ProdutoFornecedor?> GetByFornecedorAndCodigo(Guid fornecedorId, string codigoFornecedor, CancellationToken token = default) =>
        Task.FromResult(Mapeamentos.FirstOrDefault(m => m.FornecedorId == fornecedorId && m.CodigoFornecedor == codigoFornecedor));

    public Task<ProdutoFornecedor?> GetByProdutoAndFornecedor(Guid produtoId, Guid fornecedorId, CancellationToken token = default) =>
        Task.FromResult(Mapeamentos.FirstOrDefault(m => m.ProdutoId == produtoId && m.FornecedorId == fornecedorId));

    public Task UpsertMapping(Guid empresaId, Guid fornecedorId, Guid produtoId, string? codigoFornecedor, string? eanFornecedor, CancellationToken token = default) =>
        Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken token = default) => Task.CompletedTask;
}

internal sealed class InMemoryNotaFiscalRepositorio : INotaFiscalRepositorio
{
    public List<TruckFlow.Domain.Entities.NotaFiscal> Notas { get; } = new();

    public Task<TruckFlow.Domain.Entities.NotaFiscal> SaveParsedNotaAsync(TruckFlow.Domain.Entities.NotaFiscal nota, CancellationToken token)
    {
        Notas.Add(nota);
        return Task.FromResult(nota);
    }

    public Task SaveChangesAsync(CancellationToken token) => Task.CompletedTask;

    public Task<TruckFlow.Domain.Entities.NotaFiscal?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token) =>
        Task.FromResult(Notas.FirstOrDefault(n => n.ChaveAcesso == chaveAcesso));

    public Task<TruckFlow.Domain.Entities.NotaFiscal?> ObterPorChaveAcrossTenantsAsync(string chaveAcesso, CancellationToken token) =>
        Task.FromResult(Notas.FirstOrDefault(n => n.ChaveAcesso == chaveAcesso));

    public Task<Guid?> GetUltimoProdutoIdPorFornecedorECodigo(Guid fornecedorId, string codigoFornecedor, CancellationToken token) =>
        Task.FromResult<Guid?>(null);
}

internal sealed class NoOpEmpresaContext : IEmpresaContext
{
    public Guid? EmpresaIdOrNull { get; set; }
    public Guid EmpresaId => EmpresaIdOrNull ?? Guid.Empty;

    public IDisposable WithTenant(Guid empresaId) => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
