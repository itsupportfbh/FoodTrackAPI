namespace CateringApi.Models
{
    public class Cuisine
    {
        public int? Id { get; set; }
        public string CuisineName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CuisineDto
    {
        public int Id { get; set; }
        public string CuisineName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
