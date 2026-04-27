namespace CateringApi.DTOs.Scanner
{
    public class LockedPlanTypeDto
    {
        public string PlanType { get; set; } = "";
        public int ApprovalStatus { get; set; }
        public string StatusText { get; set; } = "";
    }
}
