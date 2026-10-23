namespace TruckFlow.Domain.Exceptions
{
    public class DomainException(string message) : Exception(message)
    {
        public string Code { get; } = "BUSINESS_ERROR";
    }
}
