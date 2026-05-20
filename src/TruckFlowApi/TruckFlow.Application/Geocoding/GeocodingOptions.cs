namespace TruckFlow.Application.Geocoding
{
    public sealed class GeocodingOptions
    {
        public const string SectionName = "GeocodingOptions";

        /// <summary>
        /// Obrigatório pelos termos de uso do Nominatim — identifica o app + contato.
        /// Ex: "TruckFlow/1.0 (contato@truckflow.app)".
        /// </summary>
        public string NominatimUserAgent { get; set; } = "TruckFlow/1.0";

        public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

        /// <summary>
        /// Vazio = fallback Google desabilitado, sistema usa só Nominatim.
        /// </summary>
        public string? GoogleApiKey { get; set; }

        public string GoogleBaseUrl { get; set; } = "https://maps.googleapis.com";

        public int TimeoutSeconds { get; set; } = 5;
    }
}
