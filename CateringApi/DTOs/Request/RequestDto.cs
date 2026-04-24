namespace CateringApi.Models
{
    public class RequestHeaderDto
    {
        public int? Id { get; set; }
        public string? RequestNo { get; set; }

        public int CompanyId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<RequestDetailModel> Lines { get; set; } = new();
    }

    public class RequestDetailModel
    {
        public int Id { get; set; }
        public int RequestHeaderId { get; set; }

        public string PlanType { get; set; } = string.Empty;

        public int? SessionId { get; set; }
        public int CuisineId { get; set; }
        public int? LocationId { get; set; }

        public decimal Qty { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;

        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }

        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<RequestDetailDto> Lines { get; set; } = new();

        public int OrderDays { get; set; }
    }

    public class RequestDetailDto
    {
        public int Id { get; set; }
        public int RequestHeaderId { get; set; }

        public string PlanType { get; set; } = string.Empty;

        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;

        public int CuisineId { get; set; }
        public string CuisineName { get; set; } = string.Empty;

        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;

        public decimal Qty { get; set; }
    }

    public class DropdownDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RequestPageMasterDto
    {
        public IEnumerable<DropdownDto> Companies { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Sessions { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Cuisines { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Locations { get; set; } = new List<DropdownDto>();

        public int OrderDays { get; set; }

        public string? BreakfastCutOffTime { get; set; }
        public string? LunchCutOffTime { get; set; }
        public string? LateLunchCutOffTime { get; set; }
        public string? DinnerCutOffTime { get; set; }
        public string? LateDinnerCutOffTime { get; set; }
    }
    public class SiteSettingsMasterDto
    {
        public int? OrderDays { get; set; }
        public string? BreakfastCutOffTime { get; set; }
        public string? LunchCutOffTime { get; set; }
        public string? LateLunchCutOffTime { get; set; }
        public string? DinnerCutOffTime { get; set; }
        public string? LateDinnerCutOffTime { get; set; }
    }
    public class PlanUserCountDto
    {
        public string PlanType { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}