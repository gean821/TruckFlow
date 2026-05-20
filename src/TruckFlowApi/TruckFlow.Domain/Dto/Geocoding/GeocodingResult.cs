namespace TruckFlow.Domain.Dto.Geocoding
{
    public sealed record GeocodingResult(
        double Latitude,
        double Longitude,
        string Provider,
        string? DisplayName
    );
}
