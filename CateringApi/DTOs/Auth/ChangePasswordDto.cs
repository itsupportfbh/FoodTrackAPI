namespace CateringApi.DTOs.Auth
{
    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
