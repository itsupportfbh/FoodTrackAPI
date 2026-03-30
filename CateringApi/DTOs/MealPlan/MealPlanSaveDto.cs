namespace CateringApi.DTOs.MealPlan
{
    public class MealPlanSaveDto
    {
        public int CompanyId { get; set; }
        public int MealTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Qty { get; set; }
        public string? Remarks { get; set; }
        public int? UserId { get; set; }
    }
}