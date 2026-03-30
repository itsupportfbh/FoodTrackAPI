namespace CateringApi.DTOs.Reports
{
    public class InvalidScanDto
    {
        public long Id { get; set; }
        public DateTime ScanTime { get; set; }
        public DateTime ScanDate { get; set; }
        public string? CompanyName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? MealTypeName { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }
}