namespace TruckFlow.Domain.Contracts
{
    public interface IEmpresaContext
    {
        Guid EmpresaId { get; }
        Guid? EmpresaIdOrNull { get; }

        IDisposable WithTenant(Guid empresaId);
    }
}
