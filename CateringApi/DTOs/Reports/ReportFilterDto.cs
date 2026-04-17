namespace CateringApi.Models
{
    public class ReportFilterDto
    {
        public int UserId { get; set; }

        // old single-select support
        public int? CompanyId { get; set; }
        public int? SessionId { get; set; }
        public int? CuisineId { get; set; }
        public int? LocationId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // new multi-select support
        public List<int>? CompanyIds { get; set; }
        public List<int>? SessionIds { get; set; }
        public List<int>? CuisineIds { get; set; }
        public List<int>? LocationIds { get; set; }
    }

    public class ReportByDateRowDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string CuisineName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public decimal Count { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportPageMasterDto
    {
        public IEnumerable<DropdownDto> Companies { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Sessions { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Cuisines { get; set; } = new List<DropdownDto>();
        public IEnumerable<DropdownDto> Locations { get; set; } = new List<DropdownDto>();

        public int RoleId { get; set; }
        public int DefaultCompanyId { get; set; }
        public string DefaultCompanyName { get; set; } = string.Empty;
    }

    public class FoodTotalDto
    {
        public string CuisineName { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
    }

    public class ReportEmailRequestDto : ReportFilterDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = "Report By Dates";
        public string Body { get; set; } = "Please find the attached report.";
    }
}