namespace CateringApi.DTOs
{
    public class CreateUserMasterDto
    {
        public int CompanyId { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }

        public bool IsDelete { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }

        public int? CuisinePriceId { get; set; }
        public string? PlanType { get; set; }

        public int? CuisineId { get; set; }
    }
}
