using CateringApi.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace CateringApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["SmtpSettings:SmtpHost"];
            var smtpPort = Convert.ToInt32(_configuration["SmtpSettings:SmtpPort"]);
            var smtpUser = _configuration["SmtpSettings:SmtpUser"];
            var smtpPass = _configuration["SmtpSettings:SmtpPass"];
            var fromEmail = _configuration["SmtpSettings:From"];

            if (string.IsNullOrWhiteSpace(toEmail))
                throw new Exception("Recipient email address is missing.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new Exception("SmtpSettings:From is missing in appsettings.json.");

            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new Exception("SmtpSettings:SmtpHost is missing in appsettings.json.");

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}