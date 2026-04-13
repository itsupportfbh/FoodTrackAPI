namespace CateringApi.DTOs.Company
{
    public class CompanyOrderDTO
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal RedeemQty { get; set; }
        public decimal PendingQty { get; set; }
    }
}
