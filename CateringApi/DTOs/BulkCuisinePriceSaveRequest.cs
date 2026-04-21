namespace CateringApi.DTOs
{
    public class DefaultPlanRateSaveRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public DateTime EffectiveFrom { get; set; }
        public List<PlanSessionRateDto> SessionRates { get; set; } = new();
    }

    public class PlanSessionRateDto
    {
        public int SessionId { get; set; }
        public decimal Rate { get; set; }
    }
    public class CompanyPlanRateViewDto
    {
        public string PlanType { get; set; } = string.Empty;
        public DateTime? EffectiveFrom { get; set; }
        public List<PlanSessionRateViewDto> SessionRates { get; set; } = new();
    }

    public class PlanSessionRateViewDto
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class SessionDropdownDto
    {
        public int Id { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string? FromTime { get; set; }
        public string? ToTime { get; set; }
    }

    public class CuisinePriceHistoryDto
    {
        public int Id { get; set; }
        public int PriceId { get; set; }
        public int CompanyId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public string CuisineName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PriceListDto
    {
        public int Id { get; set; }
        public int PriceId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public int CuisineId { get; set; }
        public string CuisineName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }
        public string PlanType { get; set; } = string.Empty;
    }
    public class DefaultPlanBulkSaveRequest
    {
        public int UpdatedBy { get; set; }
        public List<DefaultPlanRateSaveRequest> Plans { get; set; } = new();
    }

  

   
}