namespace CateringApi.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Mode { get; set; } = "password"; // username | password
    }
}
