namespace CateringApi.Models
{
    public class Request
    {
        public int? RequestId { get; set; }

        public int CompanyId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public int LocationId { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal Qty { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class RequestDto
    {
        public int RequestId { get; set; }

        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;

        public int CuisineId { get; set; }
        public string CuisineName { get; set; } = string.Empty;

        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal Qty { get; set; }

        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
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
    }
}