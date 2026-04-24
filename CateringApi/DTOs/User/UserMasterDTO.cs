namespace CateringApi.DTOs.User
{
    public class UserMasterDTO
    {
        public int Id { get; set; } 
        public int CompanyId { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }

        public string Email {  get; set; }

        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CompanyName { get; set; }
        public string RoleName { get; set; }

        public int? CuisinePriceId { get; set; }
        public string? PlanType { get; set; }
    }
}
