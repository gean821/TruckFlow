namespace TruckFlow.Domain.Dto.User.Motorista
{
    public class LoginMotoristaResponseDto
    {
        public required string Token { get; set; }
        public required DateTime TokenExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public required DateTime RefreshTokenExpiresAt { get; set; }
        public required UserMotoristaResponseDto Usuario { get; set; }
    }
}