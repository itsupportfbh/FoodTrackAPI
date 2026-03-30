namespace CateringApi.DTOs.Employee
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? DepartmentName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public string? QRCodeValue { get; set; }
        public bool IsActive { get; set; }
    }
}