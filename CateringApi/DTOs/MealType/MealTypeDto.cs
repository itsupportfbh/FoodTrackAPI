namespace CateringApi.DTOs.MealType
{
    public class MealTypeDto
    {
        public int Id { get; set; }
        public string MealTypeCode { get; set; } = "";
        public string MealTypeName { get; set; } = "";
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}