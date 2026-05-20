namespace TruckFlow.Domain.Dto.Auth
{
    public class RefreshResponseDto
    {
        public required string Token { get; set; }
        public required DateTime TokenExpiresAt { get; set; }
    }
}