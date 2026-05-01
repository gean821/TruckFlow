namespace TruckFlow.Domain.Contracts
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        Guid? UserIdOrNull { get; }
        Guid? EmpresaId { get; }
        bool IsAdmin { get; }
        bool IsMotorista { get; }
    }
}
