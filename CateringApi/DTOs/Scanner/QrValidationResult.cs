namespace CateringApi.DTOs.Scanner
{
    public class QrValidationResult
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }
        public string? SessionName { get; set; }
        public DateTime? ScanTime { get; set; }
    }



}
