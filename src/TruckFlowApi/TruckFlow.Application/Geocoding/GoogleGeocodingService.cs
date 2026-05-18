using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TruckFlow.Domain.Dto.Geocoding;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Geocoding
{
    public sealed class GoogleGeocodingService
    {
        public const string HttpClientName = "GoogleGeocoding";

        private readonly HttpClient _http;
        private readonly GeocodingOptions _options;
        private readonly ILogger<GoogleGeocodingService> _logger;

        public GoogleGeocodingService(
            IHttpClientFactory httpClientFactory,
            IOptions<GeocodingOptions> options,
            ILogger<GoogleGeocodingService> logger)
        {
            _http = httpClientFactory.CreateClient(HttpClientName);
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.GoogleApiKey);

        public async Task<GeocodingResult?> GeocodeAsync(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            if (!IsConfigured) {
                return null;
            } 

            var endereco = BuildAddressQuery(unidade);
            
            if (string.IsNullOrWhiteSpace(endereco)) {
                return null;
            } 

            var url = $"/maps/api/geocode/json?address={Uri.EscapeDataString(endereco)}&region=br&key={_options.GoogleApiKey}";

            try
            {
                var response = await _http.GetAsync(url, token);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Google Geocoding retornou {StatusCode} pra endereço '{Endereco}'",
                        response.StatusCode, endereco);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<GoogleResponse>(cancellationToken: token);

                if (payload is null 
                    || payload.Status != "OK" 
                    || payload.Results is null 
                    || payload.Results.Length == 0)
                {
                    _logger.LogWarning(
                        "Google Geocoding sem resultado pra '{Endereco}'. Status={Status}",
                        endereco, payload?.Status);
                    return null;
                }

                var first = payload.Results[0];
                var loc = first.Geometry?.Location;
                
                if (loc is null)
                {
                    return null;
                }

                _logger.LogInformation(
                    "Google geocodificou '{Endereco}' → ({Lat}, {Lng}) [{FormattedAddress}]",
                    endereco, loc.Lat, loc.Lng, first.FormattedAddress);

                return new GeocodingResult(loc.Lat, loc.Lng, "Google", first.FormattedAddress);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Google Geocoding falhou pra endereço '{Endereco}'", endereco);
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

        private sealed class GoogleResponse
        {
            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("results")]
            public GoogleResult[]? Results { get; set; }
        }

        private sealed class GoogleResult
        {
            [JsonPropertyName("formatted_address")]
            public string? FormattedAddress { get; set; }

            [JsonPropertyName("geometry")]
            public GoogleGeometry? Geometry { get; set; }
        }

        private sealed class GoogleGeometry
        {
            [JsonPropertyName("location")]
            public GoogleLocation? Location { get; set; }
        }

        private sealed class GoogleLocation
        {
            [JsonPropertyName("lat")]
            public double Lat { get; set; }

            [JsonPropertyName("lng")]
            public double Lng { get; set; }
        }
    }
}