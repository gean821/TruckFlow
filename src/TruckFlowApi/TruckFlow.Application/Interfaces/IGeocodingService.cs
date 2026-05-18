using TruckFlow.Domain.Dto.Geocoding;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IGeocodingService
    {
        Task<GeocodingResult?> GeocodeAsync(UnidadeEntrega unidade, CancellationToken token = default);
    }
}