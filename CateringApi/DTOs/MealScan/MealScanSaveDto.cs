namespace CateringApi.DTOs.MealScan
{
    public class MealScanSaveDto
    {
        public string EmployeeCode { get; set; } = "";
        public int MealTypeId { get; set; }
        public DateTime? ScanTime { get; set; }
        public string? QRCodeValue { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
        public int? CreatedBy { get; set; }
    }
}