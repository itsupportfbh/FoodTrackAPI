namespace CateringApi.DTOs.Scanner
{
    public class SmtpSettings
    {
        public string From { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }

    }



    public class SendEmailDto
    {
        public string Email { get; set; }
        public string? CompanyName { get; set; }
        public string? RequestNo { get; set; }
        public string? PlanType { get; set; }
        public DateTime? QrValidFrom { get; set; }
        public DateTime? QrValidTill { get; set; }
        public List<SendQrItemDto> QrItems { get; set; } = new();
    }

    public class SendQrItemDto
    {
        public string UniqueCode { get; set; }
        public string QrText { get; set; }
        public string QrImageBase64 { get; set; }
        public int? SerialNo { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedDate { get; set; }
    }

    public class SendQrEmailRequest
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int CompanyId { get; set; }
        public string Email { get; set; }
    }
}
