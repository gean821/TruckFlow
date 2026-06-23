using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TruckFlow.Contracts;

namespace TruckFlowApi.Infra.Email
{
    public sealed class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendOptions _options;

        public ResendEmailService(HttpClient httpClient, IOptions<ResendOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.resend.com/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        public async Task SendAsync(string para, string assunto, string corpoHtml, CancellationToken token = default)
        {
            var payload = new
            {
                from = $"{_options.FromName} <{_options.FromEmail}>",
                to = new[] { para },
                subject = assunto,
                html = corpoHtml
            };

            var response = await _httpClient.PostAsJsonAsync("emails", payload, token);
            response.EnsureSuccessStatusCode();
        }
    }
}
