namespace CateringApi.DTOs.User
{
    public class UserMaster
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        public int? RoleId { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }

        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public bool IsDelete { get; set; }

        public string? PlanType { get; set; }
    }
    public class UserMaster1
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int RoleId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password1 { get; set; }

        public string? PasswordHash { get; set; }

        public bool IsActive { get; set; }


        public string? PlanType { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
    public class UserBulkUploadDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;
    }

}