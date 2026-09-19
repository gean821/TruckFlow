using TruckFlow.Domain.Dto.Auth;

namespace TruckFlow.Application.Interfaces
{
    /// <summary>
    /// Provisionamento just-in-time de usuários staff (Admin/Gerente/Monitor/Portaria) autenticados
    /// via Entra ID. Nunca cria/ativa Motorista — esse papel nunca vem do Entra ID.
    /// </summary>
    public interface IJitProvisioningService
    {
        Task<JitProvisioningResult> SincronizarAsync(
            string entraObjectId,
            string? email,
            string? nomeExibicao,
            IReadOnlyCollection<string> entraGroupIds,
            CancellationToken token = default);
    }
}
