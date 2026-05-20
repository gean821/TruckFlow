using Microsoft.Extensions.Options;
using TruckFlow.Application.Geocoding;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.Geocoding
{
    public static class GeocodingDependencyInjection
    {
        public static IServiceCollection AddGeocoding(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<GeocodingOptions>(
                configuration.GetSection(GeocodingOptions.SectionName));

            services.AddHttpClient(NominatimGeocodingService.HttpClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
                client.BaseAddress = new Uri(options.NominatimBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                // Termos de uso do Nominatim exigem User-Agent identificável.
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.NominatimUserAgent);
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
            });

            services.AddHttpClient(GoogleGeocodingService.HttpClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
                client.BaseAddress = new Uri(options.GoogleBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            services.AddTransient<NominatimGeocodingService>();
            services.AddTransient<GoogleGeocodingService>();
            services.AddTransient<IGeocodingService, CompositeGeocodingService>();

            return services;
        }
    }
}