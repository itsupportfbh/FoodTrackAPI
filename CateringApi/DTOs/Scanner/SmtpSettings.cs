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
}
