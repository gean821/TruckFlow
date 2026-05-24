using System.Text.Json.Serialization;

namespace TruckFlowApi.Infra.Notifications.Expo
{
    public sealed class ExpoPushMessage
    {
        [JsonPropertyName("to")]
        public required string To { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object?>? Data { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "high";

        [JsonPropertyName("sound")]
        public string? Sound { get; set; } = "default";
    }

    public sealed class ExpoPushTicket
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public ExpoPushDetails? Details { get; set; }
    }

    public sealed class ExpoPushDetails
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("expoPushToken")]
        public string? ExpoPushToken { get; set; }
    }

    public sealed class ExpoPushSendResponse
    {
        [JsonPropertyName("data")]
        public List<ExpoPushTicket> Data { get; set; } = new();
    }

    public sealed class ExpoPushReceipt
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public ExpoPushDetails? Details { get; set; }
    }

    public sealed class ExpoPushReceiptsResponse
    {
        [JsonPropertyName("data")]
        public Dictionary<string, ExpoPushReceipt> Data { get; set; } = new();
    }
}