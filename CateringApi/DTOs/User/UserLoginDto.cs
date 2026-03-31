namespace CateringApi.DTOs.User
{
    public class UserLoginDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
