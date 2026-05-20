namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class LoginAdminResponseDto
    {
        public required string Token { get; set; }
        public required DateTime TokenExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public required DateTime RefreshTokenExpiresAt { get; set; }
        public required UserAdminResponseDto Usuario { get; set; }
    }
}