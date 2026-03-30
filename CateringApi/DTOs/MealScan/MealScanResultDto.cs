namespace CateringApi.DTOs.MealScan
{
    public class MealScanResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public int? EmployeeId { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? ScanDate { get; set; }
    }
}