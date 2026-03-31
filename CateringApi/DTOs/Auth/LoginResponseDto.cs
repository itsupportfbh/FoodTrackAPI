namespace CateringApi.DTOs.Auth
{
    public class LoginResponseDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
