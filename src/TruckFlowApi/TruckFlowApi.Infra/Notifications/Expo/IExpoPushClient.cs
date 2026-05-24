namespace TruckFlowApi.Infra.Notifications.Expo
{
    public interface IExpoPushClient
    {
        Task<List<ExpoPushTicket>> SendAsync(
            IReadOnlyList<ExpoPushMessage> messages,
            CancellationToken cancellationToken = default);

        Task<Dictionary<string, ExpoPushReceipt>> GetReceiptsAsync(
            IReadOnlyList<string> ticketIds,
            CancellationToken cancellationToken = default);
    }
}