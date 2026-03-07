using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories;

public class EmpresaRepositorio : IEmpresaRepositorio
{
    private readonly AppDbContext _db;

    public EmpresaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Empresa> CreateEmpresa(
        Empresa empresa,
        CancellationToken token = default)
    {
        await _db.Empresa.AddAsync(empresa, token);
        await SaveChangesAsync(token);

        return empresa;
    }

    public async Task<List<Empresa>> GetAll(
        CancellationToken token = default)
    {
        return await _db.Empresa
            .AsNoTracking()
            .ToListAsync(token);
    }

    public async Task<Empresa?> GetById(
        Guid id,
        CancellationToken token = default)
    {
        return await _db.Empresa
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, token);
    }

    public async Task<Empresa?> GetByCnpj(
        string cnpj,
        CancellationToken token = default)
    {
        var normalizedCnpj = NormalizeCnpj(cnpj);

        return await _db.Empresa
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Cnpj == normalizedCnpj,
                token
            );
    }

    public async Task<Empresa> Update(
        Empresa empresa,
        CancellationToken token = default)
    {
        _db.Empresa.Update(empresa);
        await SaveChangesAsync(token);

        return empresa;
    }

    public async Task Delete(
        Empresa empresa,
        CancellationToken token = default)
    {
        _db.Empresa.Remove(empresa);
        await SaveChangesAsync(token);
    }

    public async Task SaveChangesAsync(
        CancellationToken token = default)
    {
        await _db.SaveChangesAsync(token);
    }
    private static string NormalizeCnpj(string cnpj)
        => new string([.. cnpj.Where(char.IsDigit)]);
}
