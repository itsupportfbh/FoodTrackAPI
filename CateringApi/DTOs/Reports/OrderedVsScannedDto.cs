namespace CateringApi.DTOs.Reports
{
    public class OrderedVsScannedDto
    {
        public DateTime PlanDate { get; set; }
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int MealTypeId { get; set; }
        public string MealTypeCode { get; set; } = "";
        public string MealTypeName { get; set; } = "";
        public decimal FinalQty { get; set; }
        public int ScannedQty { get; set; }
        public decimal BalanceQty { get; set; }
    }
}