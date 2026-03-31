namespace CateringApi.NewFolder
{
    public class SitesettingsModel
    {
        public int Id { get; set; }

        public string BreakfastCutOffTime { get; set; }
        public string LunchCutOffTime { get; set; }
        public string LateLunchCutOffTime { get; set; }
        public string DinnerCutOffTime { get; set; }
        public string LateDinnerCutOffTime { get; set; }
        public string CronEmail { get; set; }

        public bool IsActive { get; set; } 
        public DateTime CreatedDate { get; set; } 
        public DateTime? UpdatedDate { get; set; }

        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
    }
}
