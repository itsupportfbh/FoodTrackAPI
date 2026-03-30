namespace CateringApi.DTOs.Reports
{
    public class MissedMealDto
    {
        public DateTime PlanDate { get; set; }
        public string CompanyName { get; set; } = "";
        public string MealTypeName { get; set; } = "";
        public decimal FinalQty { get; set; }
        public int ScannedQty { get; set; }
        public decimal MissedQty { get; set; }
    }
}