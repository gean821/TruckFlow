using TruckFlow.Domain.Contracts;

namespace TruckFlow.Test.Integration.Fakes;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; }
    public Guid? UserIdOrNull => UserId;
    public Guid? EmpresaId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMotorista { get; set; } = true;
    public bool IsDesenvolvedor { get; set; }
    public Guid? MotoristaId { get; set; }
    public string? NomeAutor { get; set; }
}
