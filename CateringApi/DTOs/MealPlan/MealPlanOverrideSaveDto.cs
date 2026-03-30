namespace CateringApi.DTOs.MealPlan
{
    public class MealPlanOverrideSaveDto
    {
        public int CompanyId { get; set; }
        public int MealTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Qty { get; set; }
        public string? ReasonText { get; set; }
        public int? UserId { get; set; }
    }
}