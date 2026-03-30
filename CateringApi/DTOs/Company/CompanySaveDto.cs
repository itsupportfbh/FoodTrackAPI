namespace CateringApi.DTOs.Company
{
    public class CompanySaveDto
    {
        public int? Id { get; set; }
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? ContactPerson { get; set; }
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateName { get; set; }
        public string? PostalCode { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
    }
}