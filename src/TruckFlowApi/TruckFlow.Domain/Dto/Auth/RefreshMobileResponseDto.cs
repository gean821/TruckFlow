namespace TruckFlow.Domain.Dto.Auth
{
    public class RefreshMobileResponseDto
    {
        public required string Token { get; set; }
        public required DateTime TokenExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public required DateTime RefreshTokenExpiresAt { get; set; }
    }
}