namespace CateringApi.DTOs.RequestOverride
{
    public class RequestOverrideScreenDto
    {
        public RequestOverrideHeaderDto Header { get; set; } = new();
        public List<RequestOverrideLineDto> Lines { get; set; } = new();
    }

    public class RequestOverrideHeaderDto
    {
        public int RequestHeaderId { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public DateTime RequestFromDate { get; set; }
        public DateTime RequestToDate { get; set; }
        public DateTime OverrideFromDate { get; set; }
        public DateTime OverrideToDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class RequestOverrideLineDto
    {
        public int RequestOverrideDetailId { get; set; }
        public int RequestDetailId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public int LocationId { get; set; }
        public decimal BaseQty { get; set; }
        public decimal OverrideQty { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class SaveRequestOverrideDto
    {
        public int RequestHeaderId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public List<SaveRequestOverrideLineDto> Lines { get; set; } = new();
    }

    public class SaveRequestOverrideLineDto
    {
        public int RequestDetailId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public int LocationId { get; set; }
        public decimal BaseQty { get; set; }
        public decimal OverrideQty { get; set; }
        public bool IsCancelled { get; set; }
    }
}
