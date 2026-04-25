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
    public class QrUserCountValidationDto
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; } = string.Empty;

        public int CompanyId { get; set; }
        public string PlanType { get; set; } = "Basic";

        public int RequiredCount { get; set; }
        public int AvailableUserCount { get; set; }
        public int MissingUserCount { get; set; }
    }
}