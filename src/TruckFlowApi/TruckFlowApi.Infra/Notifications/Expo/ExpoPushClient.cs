using System.Net.Http.Json;
using System.Text.Json;

namespace TruckFlowApi.Infra.Notifications.Expo
{
    public sealed class ExpoPushClient : IExpoPushClient
    {
        private const string SendPath = "/--/api/v2/push/send";
        private const string GetReceiptsPath = "/--/api/v2/push/getReceipts";

        public const int MaxSendBatchSize = 100;
        public const int MaxReceiptBatchSize = 500;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly HttpClient _httpClient;

        public ExpoPushClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ExpoPushTicket>> SendAsync(
            IReadOnlyList<ExpoPushMessage> messages,
            CancellationToken cancellationToken = default)
        {
            if (messages.Count == 0) {
                return new List<ExpoPushTicket>();
            } 

            if (messages.Count > MaxSendBatchSize)
            {
                throw new ArgumentException(
                    $"Lote excede o limite Expo de {MaxSendBatchSize} mensagens.",
                    nameof(messages));
            }

            var response = await _httpClient.PostAsJsonAsync(
                SendPath,
                messages,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ExpoPushSendResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Resposta Expo /send sem corpo.");

            return payload.Data;
        }

        public async Task<Dictionary<string, ExpoPushReceipt>> GetReceiptsAsync(
            IReadOnlyList<string> ticketIds,
            CancellationToken cancellationToken = default)
        {
            if (ticketIds.Count == 0) {
                return new Dictionary<string, ExpoPushReceipt>();
            } 

            if (ticketIds.Count > MaxReceiptBatchSize)
            {
                throw new ArgumentException(
                    $"Lote excede o limite Expo de {MaxReceiptBatchSize} receipts.",
                    nameof(ticketIds));
            }

            var body = new { ids = ticketIds };
            var response = await _httpClient.PostAsJsonAsync(GetReceiptsPath, body, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ExpoPushReceiptsResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Resposta Expo /getReceipts sem corpo.");

            return payload.Data;
        }
    }
}