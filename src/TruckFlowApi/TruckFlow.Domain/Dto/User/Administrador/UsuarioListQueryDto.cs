namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class UsuarioListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        /// <summary>
        /// "active", "inactive" ou null/all (default = todos).
        /// </summary>
        public string? Status { get; set; }
    }
}
