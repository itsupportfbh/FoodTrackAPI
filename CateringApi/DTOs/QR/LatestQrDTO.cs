namespace CateringApi.DTOs.QR
{
    public class LatestQrDTO
    {
        public string CompanyName { get; set; } = string.Empty;
        public string UniqueCode { get; set; } = string.Empty;
        public DateTime? UsedDate { get; set; }
        public string SessionName { get; set; } = string.Empty;
    }
}
