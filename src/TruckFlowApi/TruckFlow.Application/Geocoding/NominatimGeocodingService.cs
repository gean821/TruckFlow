using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TruckFlow.Domain.Dto.Geocoding;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Geocoding
{
    public sealed class NominatimGeocodingService
    {
        public const string HttpClientName = "Nominatim";

        private readonly HttpClient _http;
        private readonly GeocodingOptions _options;
        private readonly ILogger<NominatimGeocodingService> _logger;

        public NominatimGeocodingService(
            IHttpClientFactory httpClientFactory,
            IOptions<GeocodingOptions> options,
            ILogger<NominatimGeocodingService> logger)
        {
            _http = httpClientFactory.CreateClient(HttpClientName);
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GeocodingResult?> GeocodeAsync(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            var endereco = BuildAddressQuery(unidade);
            
            if (string.IsNullOrWhiteSpace(endereco))
            {
                _logger.LogDebug("Nominatim: endereço vazio pra UnidadeEntregaId={Id}, pulando.", unidade.Id);
                return null;
            }

            var url = $"/search?q={Uri.EscapeDataString(endereco)}&format=json&limit=1&countrycodes=br";

            try
            {
                var response = await _http.GetAsync(url, token);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Nominatim retornou {StatusCode} pra endereço '{Endereco}'",
                        response.StatusCode, endereco);
                    return null;
                }

                var results = await response.Content.ReadFromJsonAsync<NominatimResult[]>(cancellationToken: token);
                var first = results?.FirstOrDefault();

                if (first is null
                    || !double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                    || !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    return null;
                }

                _logger.LogInformation(
                    "Nominatim geocodificou '{Endereco}' → ({Lat}, {Lng}) [{DisplayName}]",
                    endereco, lat, lng, first.DisplayName);

                return new GeocodingResult(lat, lng, "Nominatim", first.DisplayName);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Nominatim falhou pra endereço '{Endereco}'", endereco);
                return null;
            }
        }

        private static string BuildAddressQuery(UnidadeEntrega u)
        {
            var partes = new List<string>(8);

            if (!string.IsNullOrWhiteSpace(u.Logradouro))
            {
                var rua = string.IsNullOrWhiteSpace(u.Numero)
                    ? u.Logradouro
                    : $"{u.Logradouro}, {u.Numero}";
                partes.Add(rua);
            }

            if (!string.IsNullOrWhiteSpace(u.Bairro)) partes.Add(u.Bairro);
            if (!string.IsNullOrWhiteSpace(u.Cidade)) partes.Add(u.Cidade);
            if (!string.IsNullOrWhiteSpace(u.Estado)) partes.Add(u.Estado);
            if (!string.IsNullOrWhiteSpace(u.Cep)) partes.Add(u.Cep);

            return string.Join(", ", partes);
        }

        private sealed class NominatimResult
        {
            [JsonPropertyName("lat")]
            public string? Lat { get; set; }

            [JsonPropertyName("lon")]
            public string? Lon { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }
        }
    }
}