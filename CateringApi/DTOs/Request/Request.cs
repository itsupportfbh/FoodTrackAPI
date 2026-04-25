namespace CateringApi.Models
{
    public class RequestHeader
    {
        public int Id { get; set; }
        public string? RequestNo { get; set; }

        public int CompanyId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }

        public bool IsActive { get; set; } = true;
       // public int? UserId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string PlanType { get; set; }

    }

 
}