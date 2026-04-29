namespace CateringApi.DTOs.MealPlan
{
    public class MealRequestEligibilityDto
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; } = "";
        public DateTime? MinFromDate { get; set; }
        public DateTime? MaxToDate { get; set; }
    }
}
