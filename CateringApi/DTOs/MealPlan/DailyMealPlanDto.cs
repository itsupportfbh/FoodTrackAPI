namespace CateringApi.DTOs.MealPlan
{
    public class DailyMealPlanDto
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int MealTypeId { get; set; }
        public string MealTypeCode { get; set; } = "";
        public string MealTypeName { get; set; } = "";
        public DateTime PlanDate { get; set; }
        public decimal BaseQty { get; set; }
        public decimal? OverrideQty { get; set; }
        public decimal FinalQty { get; set; }
    }
}