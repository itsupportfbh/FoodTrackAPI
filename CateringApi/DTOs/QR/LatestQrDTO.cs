namespace CateringApi.DTOs.QR
{
    public class LatestQrDTO
    {
        public string CompanyName { get; set; }
        public string UniqueCode { get; set; }
        public DateTime? UsedDate { get; set; }
        public string SessionName { get; set; }
    }
}
