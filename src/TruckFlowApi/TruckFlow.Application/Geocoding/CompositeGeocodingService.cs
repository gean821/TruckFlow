using Microsoft.Extensions.Logging;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Geocoding;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Geocoding
{
    /// <summary>
    /// Orquestra Nominatim (primário) → Google (fallback se configurado e Nominatim falhou).
    /// Tenta gratuito antes do pago, escalando só quando precisa.
    /// </summary>
    public sealed class CompositeGeocodingService : IGeocodingService
    {
        private readonly NominatimGeocodingService _nominatim;
        private readonly GoogleGeocodingService _google;
        private readonly ILogger<CompositeGeocodingService> _logger;

        public CompositeGeocodingService(
            NominatimGeocodingService nominatim,
            GoogleGeocodingService google,
            ILogger<CompositeGeocodingService> logger)
        {
            _nominatim = nominatim;
            _google = google;
            _logger = logger;
        }

        public async Task<GeocodingResult?> GeocodeAsync(UnidadeEntrega unidade, CancellationToken token = default)
        {
            var nominatim = await _nominatim.GeocodeAsync(unidade, token);
            if (nominatim is not null)
            {
                return nominatim;
            }

            if (!_google.IsConfigured)
            {
                _logger.LogInformation(
                    "Geocoding: Nominatim sem resultado e Google não configurado (GoogleApiKey vazia). UnidadeEntregaId={Id}",
                    unidade.Id);
                return null;
            }

            _logger.LogInformation(
                "Geocoding: fallback Google ativado pra UnidadeEntregaId={Id}",
                unidade.Id);
            return await _google.GeocodeAsync(unidade, token);
        }
    }
}