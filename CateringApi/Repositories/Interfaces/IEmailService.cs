namespace CateringApi.Repositories.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] fileBytes, string fileName);
    }
}
